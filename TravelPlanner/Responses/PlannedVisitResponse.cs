namespace TravelPlanner.Responses;

public record PlannedVisitResponse(
    string Name,
    DateTime ArrivalTime,
    DateTime DepartureTime);