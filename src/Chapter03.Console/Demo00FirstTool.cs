using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace Chapter03.ConsoleApp;

internal static class Demo00FirstTool
{
    public static async Task RunAsync()
    {
        AIAgent weatherAgent = AgentFactory.CreateAgent(
            "You are a helpful weather assistant.",
            "WeatherBot",
            AIFunctionFactory.Create(GetCurrentWeather),
            AIFunctionFactory.Create(ConvertToFahrenheit));

        Console.WriteLine(await weatherAgent.RunAsync("What's the weather in London in Fahrenheit?"));
    }

    [Description("Gets the current weather for a given city")]
    private static string GetCurrentWeather([Description("The city to get the weather for")] string city) =>
        city.ToLowerInvariant() switch
        {
            "antwerp" => "Cloudy, 14 degrees Celsius",
            "london" => "Rainy, 11 degrees Celsius",
            "tokyo" => "Sunny, 23 degrees Celsius",
            _ => $"Unknown weather for {city}"
        };

    [Description("Converts a temperature from Celsius to Fahrenheit")]
    private static string ConvertToFahrenheit([Description("The temperature in Celsius")] double celsius)
    {
        double fahrenheit = (celsius * 9 / 5) + 32;
        return $"{fahrenheit:F1} degrees Fahrenheit";
    }
}