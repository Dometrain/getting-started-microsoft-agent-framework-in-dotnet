using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace Chapter03.ConsoleApp;

internal static class AgentFactory
{
    public static OpenAIClient CreateClient()
    {
        const string endpoint = "https://models.github.ai/inference";
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");

        return new OpenAIClient(
            new ApiKeyCredential(githubToken),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
    }

    public static AIAgent CreateAgent(string instructions, string name, params AITool[] tools) =>
        CreateClient().GetChatClient("gpt-4o-mini").AsIChatClient().AsAIAgent(
            instructions: instructions,
            name: name,
            tools: tools);
}