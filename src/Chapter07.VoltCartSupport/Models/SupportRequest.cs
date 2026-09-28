namespace VoltCartSupport.Models;

public record SupportRequest(string Message, bool CustomerConfirmed = false);
