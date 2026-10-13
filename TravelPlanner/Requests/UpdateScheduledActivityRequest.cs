namespace TravelPlanner.Requests;

public record UpdateScheduledActivityRequest(
    DateTime StartTime,
    double SpeedKmh);