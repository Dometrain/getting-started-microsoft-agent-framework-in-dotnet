using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace Chapter03.ConsoleApp;

internal static class Demo03ToolConfirmation
{
    public static async Task RunAsync()
    {
        AIAgent assistant = AgentFactory.CreateAgent(
            "You are a helpful assistant that can draft and send email.",
            "EmailAssistant",
            AIFunctionFactory.Create(SendEmail));

        Console.WriteLine(await assistant.RunAsync("Email alex@example.com with subject Meeting and ask to meet tomorrow."));
    }

    [Description("Sends an email to the specified recipient")]
    private static string SendEmail(
        [Description("The email address of the recipient")] string to,
        [Description("The subject line of the email")] string subject,
        [Description("The body of the email")] string body)
    {
        var approved = ConfirmAction("send an email", new()
        {
            ["To"] = to,
            ["Subject"] = subject,
            ["Body"] = body
        });

        if (!approved)
            return "The user declined to send this email. Do not retry. Inform the user the email was cancelled.";

        Console.WriteLine($"Email sent to {to}");
        return $"Email successfully sent to {to}";
    }

    private static bool ConfirmAction(string description, Dictionary<string, string> details)
    {
        Console.WriteLine();
        Console.WriteLine($"The agent wants to: {description}");
        foreach (var (key, value) in details) Console.WriteLine($"   {key}: {value}");
        Console.Write("Approve? (y/n): ");
        return Console.ReadLine()?.Trim().ToLowerInvariant() == "y";
    }
}