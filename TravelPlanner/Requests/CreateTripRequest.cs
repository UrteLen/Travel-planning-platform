namespace TravelPlanner.Requests;

public record CreateTripRequest(
    string Name,
    string Destination,
    DateTime StartDate,
    DateTime EndDate,
    string OrganizerName);
