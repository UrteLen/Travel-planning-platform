namespace TravelPlanner.Responses;

public record TripResponse(
    Guid Id,
    string Name,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    Dictionary<Guid, ParticipantResponse> Participants,
    List<ActivityResponse> Activities,
    BudgetResponse Budget,
    string InviteCode);