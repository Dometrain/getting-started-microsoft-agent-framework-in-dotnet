using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace Chapter03.ConsoleApp;

internal static class Demo01MultipleTools
{
    public static async Task RunAsync()
    {
        AIAgent assistant = AgentFactory.CreateAgent(
            "You are a helpful assistant with weather, calculation, and date/time tools.",
            "Assistant",
            AIFunctionFactory.Create(GetCurrentWeather),
            AIFunctionFactory.Create(Calculate),
            AIFunctionFactory.Create(GetDateTime));

        Console.WriteLine(await assistant.RunAsync("What's the weather in London, what time is it, and what is 12 multiplied by 8?"));
    }

    [Description("Gets the current weather for a given city")]
    private static string GetCurrentWeather([Description("The city to get the weather for")] string city) =>
        city.Equals("london", StringComparison.OrdinalIgnoreCase) ? "Rainy, 11 degrees Celsius" : "Weather unavailable";

    [Description("Evaluates a simple math expression with two numbers")]
    private static string Calculate(
        [Description("The first number")] double a,
        [Description("The operation: add, subtract, multiply, or divide")] string operation,
        [Description("The second number")] double b)
    {
        var result = operation.ToLowerInvariant() switch
        {
            "add" => a + b,
            "subtract" => a - b,
            "multiply" => a * b,
            "divide" when b != 0 => a / b,
            _ => double.NaN
        };
        return $"{a} {operation} {b} = {result}";
    }

    [Description("Gets the current date and time")]
    private static string GetDateTime([Description("Optional timezone name")] string? timezone = null)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return $"Current UTC date and time: {DateTime.UtcNow:dddd, MMMM dd yyyy, HH:mm:ss}";

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            return $"Current date and time in {timezone}: {localTime:dddd, MMMM dd yyyy, HH:mm:ss}";
        }
        catch
        {
            return $"Unknown timezone: {timezone}";
        }
    }
}