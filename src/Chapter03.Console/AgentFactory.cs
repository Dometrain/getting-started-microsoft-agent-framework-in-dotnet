using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace Chapter03.ConsoleApp;

internal static class AgentFactory
{
    // GitHub Models was retired on 30 July 2026. Point OPENAI_ENDPOINT at any OpenAI-compatible
    // service (OpenAI, Azure OpenAI / Foundry, Ollama, ...); leave it unset to use api.openai.com.
    private static string ModelName => Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

    public static OpenAIClient CreateClient()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY is not set. See README.md for how to configure a model provider.");
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint)) options.Endpoint = new Uri(endpoint);
        return new OpenAIClient(new ApiKeyCredential(apiKey), options);
    }

    public static AIAgent CreateAgent(string instructions, string name, params AITool[] tools) =>
        CreateClient().GetChatClient(ModelName).AsIChatClient().AsAIAgent(
            instructions: instructions,
            name: name,
            tools: tools);
}