namespace VoltCartSupport.Security;

public static class InjectionDetector
{
    private static readonly string[] SuspiciousPatterns =
    [
        "ignore all previous",
        "ignore your instructions",
        "disregard your",
        "forget your instructions",
        "you are now",
        "new instructions:",
        "system prompt:",
        "reveal your",
        "what are your instructions",
        "list your tools",
        "act as if",
        "pretend you are",
        "jailbreak",
        "dan mode"
    ];

    public static (bool IsSuspicious, string? MatchedPattern) Check(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        var match = SuspiciousPatterns.FirstOrDefault(pattern => lowerMessage.Contains(pattern));
        return match is null ? (false, null) : (true, match);
    }
}
