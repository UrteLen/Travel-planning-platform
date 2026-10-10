using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record ExpenseResponse(
    decimal Amount,
    Category Category,
    DateTime Date,
    ParticipantResponse Participant);