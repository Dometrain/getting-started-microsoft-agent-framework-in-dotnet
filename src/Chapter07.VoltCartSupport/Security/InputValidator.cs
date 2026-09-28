namespace VoltCartSupport.Security;

public static class InputValidator
{
    private const int MaxMessageLength = 2000;
    private const int MinMessageLength = 2;

    public static (bool IsValid, string? Error) Validate(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, "Message cannot be empty.");
        }

        if (message.Length < MinMessageLength)
        {
            return (false, "Message is too short to be a valid support request.");
        }

        if (message.Length > MaxMessageLength)
        {
            return (false, $"Message exceeds the maximum length of {MaxMessageLength} characters.");
        }

        if (message.Any(character => char.IsControl(character) &&
                                     character != '\n' &&
                                     character != '\r' &&
                                     character != '\t'))
        {
            return (false, "Message contains invalid characters.");
        }

        return (true, null);
    }
}
