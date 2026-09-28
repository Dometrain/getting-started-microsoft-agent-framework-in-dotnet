namespace VoltCartSupport.Security;

public static class ContentFilter
{
    private static readonly string[] BlockedOutputPatterns =
    [
        "system prompt",
        "my instructions are",
        "i was told to",
        "here are my tools:",
        "tool parameters:"
    ];

    public static (bool IsClean, string FilteredResponse) Filter(string response)
    {
        var lowerResponse = response.ToLowerInvariant();

        foreach (var pattern in BlockedOutputPatterns)
        {
            if (lowerResponse.Contains(pattern))
            {
                return (false,
                    "I can help you with order tracking, billing, or technical support. What would you like help with?");
            }
        }

        return (true, response);
    }
}
