using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

const string endpoint = "https://models.github.ai/inference";
const string modelName = "gpt-4o-mini";
var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
    ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(githubToken),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

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