using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// GitHub Models was retired on 30 July 2026. Point OPENAI_ENDPOINT at any OpenAI-compatible
// service (OpenAI, Azure OpenAI / Foundry, Ollama, ...); leave it unset to use api.openai.com.
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY is not set. See README.md for how to configure a model provider.");
var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
var modelName = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

var clientOptions = new OpenAIClientOptions();
if (!string.IsNullOrWhiteSpace(endpoint)) clientOptions.Endpoint = new Uri(endpoint);
var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);

builder.Services.AddChatClient(openAIClient.GetChatClient(modelName).AsIChatClient());
var weatherTool = AIFunctionFactory.Create(Tools.GetCurrentWeather);
var calculatorTool = AIFunctionFactory.Create(Tools.Calculate);
AIAgent assistant = openAIClient.GetChatClient(modelName).AsIChatClient().AsAIAgent(
    instructions: "You are a helpful assistant with access to weather data and a calculator.",
    name: "Assistant",
    tools: [weatherTool, calculatorTool]);
builder.Services.AddSingleton(assistant);

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapDevUI();
Console.WriteLine("DevUI is available at: /devui");
app.Run();