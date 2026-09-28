using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ClientModel;
using System.Diagnostics;
using VoltCartSupport.Executors;
using VoltCartSupport.Models;
using VoltCartSupport.Tools;

var builder = WebApplication.CreateBuilder(args);

var activitySource = new ActivitySource("VoltCartSupport");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("VoltCartSupport"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("VoltCartSupport")
            .AddSource("Experimental.Microsoft.Agents.AI")            // agent spans
            .AddSource("Experimental.Microsoft.Agents.AI.Workflows")  // workflow spans (when not overridden)
            .AddSource("Experimental.Microsoft.Agents.AI.*")          // future-proof wildcard
            .AddOtlpExporter(config => config.Endpoint = new Uri("https://localhost:4317"))
            .AddConsoleExporter();
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("support", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please try again in a minute." },
            cancellationToken);
    };
});

// GitHub Models was retired on 30 July 2026. Point OPENAI_ENDPOINT at any OpenAI-compatible
// service (OpenAI, Azure OpenAI / Foundry, Ollama, ...); leave it unset to use api.openai.com.
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY is not set. See README.md for how to configure a model provider.");
var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
var modelName = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

var clientOptions = new OpenAIClientOptions();
if (!string.IsNullOrWhiteSpace(endpoint)) clientOptions.Endpoint = new Uri(endpoint);
var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);

IChatClient chatClient = openAiClient
    .GetChatClient(modelName)
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "VoltCartSupport", configure: (cfg) => cfg.EnableSensitiveData = true)
    .Build();

builder.Services.AddChatClient(chatClient);
builder.AddDevUI();

var triageAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "Triage",
    ChatOptions = new ChatOptions
    {
        Instructions = """
                       You are the triage agent for VoltCart customer support.
                       Analyze the CUSTOMER'S message and return a TriageDecision.
                       TargetAgent must be exactly one of: "orders", "billing", "technical".
                       Confidence is between 0.0 and 1.0.
                       Summary is a one-sentence description of the customer's issue.
                       CustomerSentiment is one of: "frustrated", "neutral", "positive".
                       Respond with ONLY the JSON object — no prose, no code fences.
                       """,
        ResponseFormat = ChatResponseFormat.ForJsonSchema<TriageDecision>(),
        Tools = [AIFunctionFactory.Create(CustomerTools.GetCustomerInfo)]
    }
})
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "Experimental.Microsoft.Agents.AI",
        configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

var ordersAgent = chatClient.AsAIAgent(
    instructions:
    """
    You are the orders specialist for VoltCart.
    Help customers with order lookups, shipping status, and cancellations.
    Be concise and helpful. Always confirm the order ID with the customer.
    If a customer wants to cancel, check the order status first.
    """,
    name: "Orders",
    tools:
    [
        AIFunctionFactory.Create(OrderTools.LookupOrder),
        AIFunctionFactory.Create(OrderTools.GetOrderStatus),
        AIFunctionFactory.Create(OrderTools.CancelOrder)
    ])
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "Experimental.Microsoft.Agents.AI",
        configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

var billingAgent = chatClient.AsAIAgent(
    instructions:
    """
    You are the billing specialist for VoltCart.
    Help customers with invoices and payment history.
    For refund requests, explain that VoltCart follows a confirmed refund workflow before submission.
    Be empathetic.
    """,
    name: "Billing",
    tools:
    [
        AIFunctionFactory.Create(BillingTools.GetInvoice),
        AIFunctionFactory.Create(BillingTools.GetPaymentHistory)
    ])
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "Experimental.Microsoft.Agents.AI",
        configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

var technicalAgent = chatClient.AsAIAgent(
    instructions:
    """
    You are the technical support specialist for VoltCart.
    Search the knowledge base first before creating a support ticket.
    Walk customers through solutions step by step.
    """,
    name: "Technical",
    tools:
    [
        AIFunctionFactory.Create(TechnicalTools.SearchKnowledgeBase),
        AIFunctionFactory.Create(TechnicalTools.CreateSupportTicket)
    ])
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "Experimental.Microsoft.Agents.AI",
        configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

builder.Services.AddAIAgent("Triage", createAgentDelegate: (sp, key) => triageAgent);
builder.Services.AddAIAgent("Orders", createAgentDelegate: (sp, key) => ordersAgent);
builder.Services.AddAIAgent("Billing", createAgentDelegate: (sp, key) => billingAgent);
builder.Services.AddAIAgent("Technical", createAgentDelegate: (sp, key) => technicalAgent);


// Register the workflow. Resolve agents from DI so their Ids line up.
builder.AddWorkflow("SupportTriage", (sp, key) =>
{
    var triage = sp.GetRequiredKeyedService<AIAgent>("Triage");
    var orders = sp.GetRequiredKeyedService<AIAgent>("Orders");
    var billing = sp.GetRequiredKeyedService<AIAgent>("Billing");
    var technical = sp.GetRequiredKeyedService<AIAgent>("Technical");

    var triageBinding = triage.BindAsExecutor(new AIAgentHostOptions
    {
        ForwardIncomingMessages = false
    });

    var parser = new TriageDecisionExecutor();
    var ordersRunner = new SpecialistExecutor("run-orders", orders);
    var billingRunner = new SpecialistExecutor("run-billing", billing);
    var techRunner = new SpecialistExecutor("run-technical", technical);

    return new WorkflowBuilder(triageBinding)
        .WithName("SupportTriage")
        .AddEdge(triageBinding, parser)
        .AddEdge(parser, ordersRunner, condition: GetCondition("orders"))
        .AddEdge(parser, billingRunner, condition: GetCondition("billing"))
        .AddEdge(parser, techRunner, condition: GetCondition("technical"))
        .WithOutputFrom(ordersRunner, billingRunner, techRunner)
        .WithOpenTelemetry(
            // Set `EnableSensitiveData` to true to include message content in traces
            configure: cfg => cfg.EnableSensitiveData = true,
            activitySource: activitySource)
        .Build();
});

static Func<object?, bool> GetCondition(string expectedResult) =>
    detectionResult => detectionResult is TriageDecision result && result.TargetAgent == expectedResult;

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.UseRateLimiter();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.UseHttpsRedirection();

Console.WriteLine("VoltCart Support is running.");
Console.WriteLine("DevUI available at: /devui");

app.Run();