using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;

namespace Chapter05.ConsoleApp;

public record DetectionResult(bool IsSpam, string Reason, string OriginalMessage);

internal static class Demos
{
    public static Task FirstWorkflowAsync()
    {
        var researcher = CreateAgent("Provide detailed factual research notes.", "Researcher");
        var summarizer = CreateAgent("Distill the notes into a concise summary.", "Summarizer");
        return RunAsync(new WorkflowBuilder(researcher).AddEdge(researcher, summarizer).Build(), "Research quantum computing.");
    }

    public static Task SequentialAsync()
    {
        var researcher = CreateAgent("Provide detailed factual research notes.", "Researcher");
        var factChecker = CreateAgent("Check every claim and flag uncertainty.", "FactChecker");
        var summarizer = CreateAgent("Produce a concise final summary.", "Summarizer");
        return RunAsync(AgentWorkflowBuilder.BuildSequential([researcher, factChecker, summarizer]), "Research practical uses of quantum computing.");
    }

    public static Task ConditionalAsync()
    {
        var detector = new SpamDetectionExecutor(CreateAgent("Classify email as spam or legitimate and explain why.", "SpamDetector"));
        var legitimate = new LegitimateEmailExecutor(CreateAgent("Draft a professional email reply.", "EmailAssistant"));
        var spam = new SpamHandlerExecutor();
        Func<object?, bool> isNotSpam = result => result is DetectionResult r && !r.IsSpam;
        Func<object?, bool> isSpam = result => result is DetectionResult r && r.IsSpam;
        var workflow = new WorkflowBuilder(detector)
            .AddEdge(detector, legitimate, condition: isNotSpam)
            .AddEdge(detector, spam, condition: isSpam)
            .WithOutputFrom(legitimate, spam)
            .Build();
        return RunAsync(workflow, "Congratulations! Claim your prize by sending your bank details.");
    }

    public static Task HumanInTheLoopAsync()
    {
        AIFunction deploy = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(DeployToProduction));
        var agent = CreateAgent("Check staging, then deploy version 2.1.0 to production.", "DeployAgent", AIFunctionFactory.Create(CheckStagingStatus), deploy);
        return RunAsync(new WorkflowBuilder(agent).WithOutputFrom(agent).Build(), "Deploy version 2.1.0.");
    }

    public static Task ConcurrentAsync()
    {
        var workflow = AgentWorkflowBuilder.BuildConcurrent([
            CreateAgent("Give a brief weather forecast.", "WeatherAgent"),
            CreateAgent("Give today's top technology headlines.", "NewsAgent"),
            CreateAgent("Give one practical productivity tip.", "ProductivityAgent")]);
        return RunAsync(workflow, "Prepare my morning briefing.");
    }

    public static Task SharedStateAndEventsAsync()
    {
        var ingestion = new EmailIngestionExecutor();
        var responder = new EmailResponderExecutor(CreateAgent("Draft a professional response.", "EmailResponder"));
        var workflow = new WorkflowBuilder(ingestion).AddEdge(ingestion, responder).WithOutputFrom(responder).Build();
        return RunAsync(workflow, "Can we move our meeting to Friday afternoon?");
    }

    private static AIAgent CreateAgent(string instructions, string name, params AITool[] tools)
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");
        var client = new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });
        return client.GetChatClient("gpt-4o-mini").AsIChatClient().AsAIAgent(instructions: instructions, name: name, tools: tools);
    }

    private static async Task RunAsync(Workflow workflow, string input)
    {
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, input));
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent update: Console.Write(update.Update.Text); break;
                case WorkflowOutputEvent output: Console.WriteLine($"\nWorkflow complete: {output.Data}"); break;
                case RequestInfoEvent request when request.Request.TryGetDataAs(out ToolApprovalRequestContent? approval):
                    Console.Write("Approve tool call? (y/n): ");
                    var approved = Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true;
                    await run.SendResponseAsync(request.Request.CreateResponse(approval.CreateResponse(approved)));
                    break;
            }
        }
    }

    [Description("Checks whether the staging deployment passed validation")]
    private static string CheckStagingStatus() => "Staging checks passed.";
    [Description("Deploys the application to production. This is irreversible.")]
    private static string DeployToProduction(string version) => $"Successfully deployed {version} to production.";
}

internal sealed class SpamDetectionExecutor(AIAgent agent) : Executor<ChatMessage, DetectionResult>("SpamDetector")
{
    public override async ValueTask<DetectionResult> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var response = await agent.RunAsync<DetectionResult>(message.Text ?? "");
        return response.Result with { OriginalMessage = message.Text ?? "" };
    }
}

internal sealed class LegitimateEmailExecutor(AIAgent agent) : Executor<DetectionResult>("LegitimateEmail")
{
    public override async ValueTask HandleAsync(DetectionResult message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var response = await agent.RunAsync($"Draft a reply to: {message.OriginalMessage}");
        await context.YieldOutputAsync($"Reply: {response.Text}");
    }
}

internal sealed class SpamHandlerExecutor() : Executor<DetectionResult>("SpamHandler")
{
    public override async ValueTask HandleAsync(DetectionResult message, IWorkflowContext context, CancellationToken cancellationToken = default) =>
        await context.YieldOutputAsync($"Spam marked: {message.Reason}");
}

internal sealed class EmailIngestionExecutor() : Executor<ChatMessage, string>("EmailIngestion")
{
    public override async ValueTask<string> HandleAsync(ChatMessage message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await context.QueueStateUpdateAsync(id, message.Text ?? "", scopeName: "EmailStore");
        await context.AddEventAsync(new ProcessingStageEvent("Email stored"));
        return id;
    }
}

internal sealed class EmailResponderExecutor(AIAgent agent) : Executor<string>("EmailResponder")
{
    public override async ValueTask HandleAsync(string id, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var email = await context.ReadStateAsync<string>(id, scopeName: "EmailStore") ?? throw new InvalidOperationException("Email not found");
        var response = await agent.RunAsync($"Draft a reply to:\n{email}");
        await context.AddEventAsync(new ProcessingStageEvent("Reply drafted"));
        await context.YieldOutputAsync($"Reply: {response.Text}");
    }
}

internal sealed class ProcessingStageEvent(string stage) : WorkflowEvent(stage);