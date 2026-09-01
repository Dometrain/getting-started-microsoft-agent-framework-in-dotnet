using System.ComponentModel;

public static class Tools
{
    [Description("Gets the current weather for a given city")]
    public static string GetCurrentWeather([Description("The city to get the weather for")] string city)
    {
        var weatherData = new Dictionary<string, string>
        {
            ["London"] = "14 degrees Celsius, cloudy",
            ["New York"] = "22 degrees Celsius, sunny",
            ["Tokyo"] = "18 degrees Celsius, light rain"
        };
        return weatherData.TryGetValue(city, out var weather)
            ? $"The weather in {city} is {weather}."
            : $"Sorry, I don't have weather data for {city}.";
    }

    [Description("Performs a basic math calculation with two numbers")]
    public static string Calculate(double a, string operation, double b)
    {
        var result = operation.ToLowerInvariant() switch
        {
            "add" => a + b,
            "subtract" => a - b,
            "multiply" => a * b,
            "divide" when b != 0 => a / b,
            "divide" => double.NaN,
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };
        return $"{a} {operation} {b} = {result}";
    }
}