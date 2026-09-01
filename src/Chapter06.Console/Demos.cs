using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Chapter06.ConsoleApp;

internal static class Demos
{
    public static async Task TracingAsync()
    {
        using var source = new ActivitySource("AgentTracing");
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgentTracingDemo"))
            .AddSource("AgentTracing").AddSource("Microsoft.Agents.*")
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .Build();
        AIAgent agent = CreateAgent("You are a helpful weather assistant.", "WeatherBot", AIFunctionFactory.Create(GetWeather));
        AgentSession session = await agent.CreateSessionAsync();
        foreach (var question in new[] { "What's the weather in Antwerp?", "What should I wear?" })
        {
            using var activity = source.StartActivity($"UserTurn: {question}");
            activity?.SetTag("user.message", question);
            activity?.SetTag("agent.name", "WeatherBot");
            var response = await agent.RunAsync(question, session);
            Console.WriteLine(response);
            activity?.SetTag("agent.response_length", response.Text.Length);
        }
    }

    public static async Task TokenMonitoringAsync()
    {
        AIAgent agent = CreateAgent("Answer concisely.", "Assistant");
        AgentSession session = await agent.CreateSessionAsync();
        var tracker = new TokenTracker(2000);
        foreach (var question in new[] { "Explain agents.", "How do tools help?", "Summarize our conversation." })
        {
            if (tracker.BudgetExceeded) { Console.WriteLine($"Budget exceeded. Not sending: {question}"); continue; }
            var response = await agent.RunAsync(question, session);
            tracker.Record(
                question,
                checked((int)(response.Usage?.InputTokenCount ?? question.Length / 4)),
                checked((int)(response.Usage?.OutputTokenCount ?? response.Text.Length / 4)));
            Console.WriteLine(response);
        }
        tracker.PrintSummary();
        tracker.PrintCostEstimate();
    }

    public static async Task PromptInjectionDefenseAsync()
    {
        AIFunction lookup = AIFunctionFactory.Create(LookupOrder);
        AIFunction discount = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ApplyDiscount));
        AIAgent agent = CreateAgent("You are a Contoso support agent. Never promise a discount until the tool succeeds.", "ContosoSupport", lookup, discount);
        AgentSession session = await agent.CreateSessionAsync();
        Console.WriteLine("Enter a support request (blank to exit).");
        while (true)
        {
            Console.Write("Customer: ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) break;
            if (input.Length > 500) { Console.WriteLine("Support: Please keep your message under 500 characters."); continue; }
            AgentResponse response = await agent.RunAsync(input, session, options: new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = 400 }));
            response = await HandleApprovalsAsync(response, agent, session);
            Console.WriteLine($"Support: {Regex.Replace(response.Text, "<[^>]+>", string.Empty)}");
        }
    }

    public static async Task MiddlewareAsync()
    {
        AIAgent baseAgent = CreateAgent("You are a customer support agent for Contoso Electronics.", "ContosoSupport", AIFunctionFactory.Create(LookupOrder));
        AIAgent productionAgent = baseAgent.AsBuilder()
            .Use(runFunc: GuardrailMiddleware, runStreamingFunc: null)
            .Use(runFunc: DisclaimerMiddleware, runStreamingFunc: null)
            .Use(AuditFunctionMiddleware)
            .Build();
        AgentSession session = await productionAgent.CreateSessionAsync();
        Console.WriteLine(await productionAgent.RunAsync("What's the status of order ORD-001?", session));
    }

    private static AIAgent CreateAgent(string instructions, string name, params AITool[] tools)
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");
        var client = new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });
        return client.GetChatClient("gpt-4o-mini").AsIChatClient().AsAIAgent(instructions: instructions, name: name, tools: tools);
    }

    private static async Task<AgentResponse> HandleApprovalsAsync(AgentResponse response, AIAgent agent, AgentSession session)
    {
        while (true)
        {
            var requests = response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToList();
            if (requests.Count == 0) return response;
            foreach (var request in requests)
            {
                Console.Write("Approve tool call? (y/n): ");
                var approved = Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
                response = await agent.RunAsync(new ChatMessage(ChatRole.User, [request.CreateResponse(approved)]), session);
            }
        }
    }

    private static async Task<AgentResponse> GuardrailMiddleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
    {
        var input = messages.LastOrDefault()?.Text?.ToLowerInvariant() ?? "";
        foreach (var word in new[] { "password", "secret", "credentials", "api key" })
            if (input.Contains(word)) return new AgentResponse([new ChatMessage(ChatRole.Assistant, $"Sorry, I cannot process requests related to '{word}'.")]);
        var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);
        return response.Text.Length <= 5000 ? response : new AgentResponse([new ChatMessage(ChatRole.Assistant, response.Text[..5000] + "\n\n... [truncated]")]);
    }

    private static async Task<AgentResponse> DisclaimerMiddleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
    {
        var response = await innerAgent.RunAsync(messages, session, options, cancellationToken);
        return new AgentResponse(response.Messages.Select(m => m.Role == ChatRole.Assistant && m.Text is not null ? new ChatMessage(ChatRole.Assistant, m.Text + "\n\nNote: AI-generated content may not be accurate.") : m).ToList());
    }

    private static async ValueTask<object?> AuditFunctionMiddleware(AIAgent agent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[AUDIT] Function: {context.Function.Name}");
        var result = await next(context, cancellationToken);
        Console.WriteLine($"[AUDIT] Result: {result}");
        return result;
    }

    [Description("Gets the current weather for a city")]
    private static string GetWeather(string city) => $"Weather in {city}: cloudy, 14 degrees Celsius.";

    [Description("Looks up a customer's order by order ID")]
    private static string LookupOrder(string orderId)
    {
        var id = orderId.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(id, "^ORD-[0-9]{3}$")) return "Error: Order IDs must match ORD-123.";
        return id switch { "ORD-001" => "Order ORD-001: Surface Laptop, delivered.", "ORD-002" => "Order ORD-002: Xbox Controller, processing.", _ => $"Order {id} was not found." };
    }

    [Description("Applies a discount to a customer's order")]
    private static string ApplyDiscount(string orderId, int percentage)
    {
        var id = orderId.Trim().ToUpperInvariant();
        if (!Regex.IsMatch(id, "^ORD-[0-9]{3}$")) return "Error: Invalid order ID.";
        if (percentage is < 1 or > 10) return "Error: Discounts must be between 1 and 10 percent.";
        return $"Discount of {percentage}% approved for {id}.";
    }
}

internal sealed record TokenUsageRecord(string Turn, int InputTokens, int OutputTokens);
internal sealed class TokenTracker(int sessionBudget = int.MaxValue)
{
    private readonly List<TokenUsageRecord> records = [];
    public int TotalInputTokens => records.Sum(r => r.InputTokens);
    public int TotalOutputTokens => records.Sum(r => r.OutputTokens);
    public int TotalTokens => TotalInputTokens + TotalOutputTokens;
    public bool BudgetExceeded => TotalTokens >= sessionBudget;
    public void Record(string turn, int input, int output) => records.Add(new(turn, input, output));
    public void PrintSummary() => Console.WriteLine($"Tokens - input: {TotalInputTokens}, output: {TotalOutputTokens}, total: {TotalTokens}");
    public void PrintCostEstimate()
    {
        var cost = TotalInputTokens / 1000m * 0.00015m + TotalOutputTokens / 1000m * 0.0006m;
        Console.WriteLine($"Estimated cost: ${cost:F6}");
    }
}