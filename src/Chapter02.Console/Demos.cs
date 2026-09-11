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
        AIAgent joker = CreateChatClient().AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
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
        AIAgent joker = CreateChatClient().AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
        AgentSession session = await joker.CreateSessionAsync();
        await ConversationLoopAsync(joker, session);
    }

    public static async Task PersistingSessionsAsync()
    {
        AIAgent joker = CreateChatClient().AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker");
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
        IChatClient chatClient = CreateChatClient();
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

    private static IChatClient CreateChatClient()
    {
        // GitHub Models was retired on 30 July 2026. Point OPENAI_ENDPOINT at any OpenAI-compatible
        // service (OpenAI, Azure OpenAI / Foundry, Ollama, ...); leave it unset to use api.openai.com.
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set. See README.md for how to configure a model provider.");
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint)) options.Endpoint = new Uri(endpoint);
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient();
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