namespace TravelPlanner.Requests;

public record JoinTripRequest(
    string InviteCode,
    string ParticipantName,
    bool LinkToAccount = false);
