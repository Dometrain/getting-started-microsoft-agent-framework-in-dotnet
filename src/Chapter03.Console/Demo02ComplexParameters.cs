using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace Chapter03.ConsoleApp;

public record BookingRequest(
    [Description("The name of the meeting room to book")] string RoomName,
    [Description("The date for the booking in ISO format")] DateOnly Date,
    [Description("Start time of the meeting")] TimeOnly StartTime,
    [Description("End time of the meeting")] TimeOnly EndTime,
    [Description("Number of people attending, minimum 1")] int Attendees,
    [Description("List of required equipment")] string[] RequiredEquipment);

internal static class Demo02ComplexParameters
{
    public static async Task RunAsync()
    {
        AIAgent assistant = AgentFactory.CreateAgent(
            "You are a helpful office assistant that can book meeting rooms.",
            "OfficeAssistant",
            AIFunctionFactory.Create(BookMeetingRoom));

        Console.WriteLine(await assistant.RunAsync("Book the large conference room for tomorrow from 2pm to 3pm for 8 people with a projector."));
    }

    [Description("Books a meeting room with the specified details")]
    private static string BookMeetingRoom(BookingRequest request)
    {
        Console.WriteLine($"  [Tool called with: Room={request.RoomName}, Date={request.Date}, Start={request.StartTime}, End={request.EndTime}, Attendees={request.Attendees}, Equipment=[{string.Join(", ", request.RequiredEquipment)}]]");
        var errors = ValidateBooking(request);
        if (errors.Count > 0)
            return $"Booking failed. Issues: {string.Join("; ", errors)}";

        return $"Booked {request.RoomName} on {request.Date} from {request.StartTime} to {request.EndTime} for {request.Attendees} attendees. Equipment: {string.Join(", ", request.RequiredEquipment)}.";
    }

    private static List<string> ValidateBooking(BookingRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.RoomName)) errors.Add("Room name is required");
        if (request.Date < DateOnly.FromDateTime(DateTime.Today)) errors.Add("Cannot book a room in the past");
        if (request.EndTime <= request.StartTime) errors.Add("End time must be after start time");
        if (request.Attendees < 1) errors.Add("At least one attendee is required");
        if (request.Attendees > 50) errors.Add("Maximum capacity is 50 attendees");
        return errors;
    }
}