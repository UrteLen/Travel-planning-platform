namespace TravelPlanner.Requests;

public record AddActivityRequest(
    string Name,
    string Category,
    double Latitude,
    double Longitude,
    double DurationHours,
    decimal EstimatedCost = 0m,
    bool Optional = true);