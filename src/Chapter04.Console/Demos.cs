using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Chapter04.ConsoleApp;

public record JokeResponse([Description("The setup line of the joke")] string Setup, [Description("The punchline of the joke")] string Punchline, [Description("The category of humor")] string Category);
public record MovieRecommendation(string Title, int Year, string Reason);
public record ReviewAnalysis([Required] string Sentiment, [Range(0.0, 1.0)] double Confidence, [Required, MaxLength(200)] string Summary, [MinLength(1)] string[] Topics);
public enum Sentiment { Positive, Negative, Mixed }
public record ReviewAnalysisV2(Sentiment Sentiment, [Range(0.0, 1.0)] double Confidence, [Required, MaxLength(200)] string Summary, [MinLength(1)] string[] Topics);
public record TripItinerary(string City, string Overview, DayPlan[] Days, string[] PackingSuggestions);
public record DayPlan(string Date, string Weather, Activity[] Activities);
public record Activity(string Name, string Category, string TimeOfDay);

internal static class Demos
{
    public static async Task BasicsAsync()
    {
        AIAgent joker = CreateAgent("You are good at telling jokes.", "Joker");
        var response = await joker.RunAsync<JokeResponse>("Tell me a joke about programming");
        Console.WriteLine($"Setup: {response.Result.Setup}");
        Console.WriteLine($"Punchline: {response.Result.Punchline}");
        Console.WriteLine($"Category: {response.Result.Category}");

        AIAgent movieBot = CreateAgent("Recommend one movie matching the user's preferences.", "MovieBot");
        AgentSession session = await movieBot.CreateSessionAsync();
        var first = await movieBot.RunAsync<MovieRecommendation>("I love sci-fi movies with mind-bending plots", session);
        var second = await movieBot.RunAsync<MovieRecommendation>("Something similar but more recent", session);
        Console.WriteLine($"{first.Result.Title} ({first.Result.Year}): {first.Result.Reason}");
        Console.WriteLine($"{second.Result.Title} ({second.Result.Year}): {second.Result.Reason}");
    }

    public static async Task ValidationAsync()
    {
        AIAgent agent = CreateAgent("Analyze the supplied customer review.", "ReviewAnalyzer");
        var result = await RunWithRetryAsync<ReviewAnalysisV2>(agent, "The battery is excellent, but the screen is too dim.");
        Console.WriteLine($"{result.Sentiment} ({result.Confidence:P0}): {result.Summary}");
    }

    public static async Task TemplatesAsync()
    {
        var template = """
            You are a {{domain}} expert who specializes in {{specialty}}.
            Respond in a {{tone}} tone.
            Your target audience is {{audience}}.
            Always provide practical, actionable advice.
            """;
        string instructions = RenderTemplate(template, new()
        {
            ["domain"] = "cooking", ["specialty"] = "Italian cuisine",
            ["tone"] = "friendly and encouraging", ["audience"] = "home cooks"
        });
        Console.WriteLine(await CreateAgent(instructions, "CookingExpert").RunAsync("How can I improve tomato sauce?"));
    }

    public static async Task ToolsAndStructuredOutputAsync()
    {
        AIAgent planner = CreateAgent(
            "Check weather for each day and find activities before creating a complete itinerary.",
            "TripPlanner",
            AIFunctionFactory.Create(GetWeatherForecast),
            AIFunctionFactory.Create(GetActivities));
        var response = await planner.RunAsync<TripItinerary>("Plan a 3-day trip to Barcelona starting April 15, 2026.");
        Console.WriteLine($"{response.Result.City}: {response.Result.Overview}");
        foreach (var day in response.Result.Days) Console.WriteLine($"{day.Date}: {day.Weather} - {string.Join(", ", day.Activities.Select(a => a.Name))}");
    }

    private static AIAgent CreateAgent(string instructions, string name, params AITool[] tools)
    {
        // GitHub Models was retired on 30 July 2026. Point OPENAI_ENDPOINT at any OpenAI-compatible
        // service (OpenAI, Azure OpenAI / Foundry, Ollama, ...); leave it unset to use api.openai.com.
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY is not set. See README.md for how to configure a model provider.");
        var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        var options = new OpenAIClientOptions();
        if (!string.IsNullOrWhiteSpace(endpoint)) options.Endpoint = new Uri(endpoint);
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        return client.GetChatClient(model).AsIChatClient().AsAIAgent(instructions: instructions, name: name, tools: tools);
    }

    private static List<ValidationResult> Validate<T>(T instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance!, new ValidationContext(instance!), results, true);
        return results;
    }

    private static async Task<T> RunWithRetryAsync<T>(AIAgent agent, string message, int maxRetries = 2)
    {
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await agent.RunAsync<T>(message);
                if (Validate(response.Result).Count == 0) return response.Result;
                Console.WriteLine($"Attempt {attempt + 1}: validation failed");
            }
            catch (Exception ex) { Console.WriteLine($"Attempt {attempt + 1}: error - {ex.Message}"); }
        }
        throw new InvalidOperationException($"Failed after {maxRetries + 1} attempts");
    }

    private static string RenderTemplate(string template, Dictionary<string, object> values)
    {
        var result = template;
        foreach (var pair in values) result = result.Replace($"{{{{{pair.Key}}}}}", pair.Value?.ToString() ?? "");
        return result;
    }

    [Description("Gets the weather forecast for a city on a specific date")]
    private static string GetWeatherForecast(string city, string date) =>
        $"{city} on {date}: Sunny, 22 degrees Celsius, light breeze";

    [Description("Gets popular activities and attractions for a city")]
    private static string GetActivities(string city, string? category = null) =>
        $"Activities in {city}: architecture tour, local market, city park" + (category is null ? "" : $"; category: {category}");
}