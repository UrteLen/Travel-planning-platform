namespace TravelPlanner.Requests;

public record ScheduleActivityRequest(
    Guid ActivityId,
    DateTime StartTime,
    double SpeedKmh);