using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// GitHub Models was retired on 30 July 2026. Point OPENAI_ENDPOINT at any OpenAI-compatible
// service (OpenAI, Azure OpenAI / Foundry, Ollama, ...); leave it unset to use api.openai.com.
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set. See README.md for how to configure a model provider.");
var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
var options = new OpenAIClientOptions();
if (!string.IsNullOrWhiteSpace(endpoint)) options.Endpoint = new Uri(endpoint);
var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
IChatClient chatClient = client.GetChatClient(model).AsIChatClient();

builder.Services.AddChatClient(chatClient);
builder.Services.AddSingleton(chatClient.AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker"));
builder.Services.AddSingleton(chatClient.AsAIAgent(instructions: "Translate any input to French.", name: "Translator"));

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapDevUI();
Console.WriteLine("DevUI is available at: /devui");
app.Run();