using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);
var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN is not set.");
var client = new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") });
IChatClient chatClient = client.GetChatClient("gpt-4o-mini").AsIChatClient();
builder.Services.AddChatClient(chatClient);
builder.Services.AddSingleton(chatClient.AsAIAgent(instructions: "You are good at telling jokes.", name: "Joker"));
builder.Services.AddSingleton(chatClient.AsAIAgent(instructions: "Translate any input to French.", name: "Translator"));

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapDevUI();
Console.WriteLine("DevUI is available at: /devui");
app.Run();