#pragma warning disable MAAI001

using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;
using StackExchange.Redis;
using System.ClientModel;
using System.Text.Json;

namespace Chapter02.ConsoleApp;

internal static class Demos
{
    public static async Task FirstAgentAsync()
    {
        AIAgent joker = CreateGitHubChatClient().AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
        Console.WriteLine(await joker.RunAsync("Tell me a joke about a pirate."));
    }

    public static async Task FoundryAgentAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT is not set.");
        var model = Environment.GetEnvironmentVariable("AZURE_AI_MODEL") ?? "gpt-4o-mini";
        AIAgent joker = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
            .AsAIAgent(model: model, instructions: "You are good at telling jokes.", name: "Joker");
        Console.WriteLine(await joker.RunAsync("Tell me a joke about a pirate."));
    }

    public static async Task LocalAgentAsync()
    {
        const string localEndpoint = "http://localhost:11434";
        const string modelName = "ministral-3";
        AIAgent localAgent = new OllamaApiClient(new Uri(localEndpoint), modelName)
            .AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
        Console.WriteLine(await localAgent.RunAsync("Tell me a joke about a pirate."));
    }

    public static async Task MultiTurnAsync()
    {
        AIAgent joker = CreateGitHubChatClient().AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
        AgentSession session = await joker.CreateSessionAsync();
        await ConversationLoopAsync(joker, session);
    }

    public static async Task PersistingSessionsAsync()
    {
        AIAgent joker = CreateGitHubChatClient().AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
        const string sessionFile = "session.json";
        AgentSession session = File.Exists(sessionFile)
            ? await joker.DeserializeSessionAsync(JsonElement.Parse(await File.ReadAllTextAsync(sessionFile)))
            : await joker.CreateSessionAsync();

        await ConversationLoopAsync(joker, session, async () =>
        {
            JsonElement serialized = await joker.SerializeSessionAsync(session);
            await File.WriteAllTextAsync(sessionFile, serialized.GetRawText());
        });
    }

    public static async Task CompactionAsync()
    {
        IChatClient chatClient = CreateGitHubChatClient();
        PipelineCompactionStrategy pipeline = new(
            new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(100)),
            new SummarizationCompactionStrategy(chatClient, CompactionTriggers.TokensExceed(200)),
            new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(4)),
            new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(300)));

        AIAgent joker = chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "Joker",
            ChatOptions = new() { Instructions = "You are good at telling jokes." },
            AIContextProviders = [new CompactionProvider(pipeline)]
        });
        AgentSession session = await joker.CreateSessionAsync();
        await ConversationLoopAsync(joker, session);
    }

    private static IChatClient CreateGitHubChatClient()
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");
        var client = new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });
        return client.GetChatClient("gpt-4o-mini").AsIChatClient();
    }

    private static async Task ConversationLoopAsync(AIAgent agent, AgentSession session, Func<Task>? afterTurn = null)
    {
        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) break;
            AgentResponse response = await agent.RunAsync(input, session);
            Console.WriteLine($"Joker: {response}");
            Console.WriteLine($"  [Input tokens: {response.Usage?.InputTokenCount} | Output tokens: {response.Usage?.OutputTokenCount}]");
            if (afterTurn is not null) await afterTurn();
        }
    }
}