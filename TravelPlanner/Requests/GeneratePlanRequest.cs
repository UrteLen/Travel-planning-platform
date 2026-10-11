namespace TravelPlanner.Requests;

public record GeneratePlanRequest(
    double StartLatitude,
    double StartLongitude,
    DateTime StartTime,
    double SpeedKmh,
    string ChangedBy);