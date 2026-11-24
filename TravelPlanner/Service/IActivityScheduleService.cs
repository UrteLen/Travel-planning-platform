using TravelPlanner.Models;

namespace TravelPlanner.Service;

public interface IActivityScheduleService
{
    Task<List<ScheduledActivity>?> GetScheduleAsync(
        Guid tripId,
        DateTime? date);

    Task<ScheduledActivity> ScheduleActivityAsync(
        Guid tripId,
        Guid activityId,
        DateTime startTime,
        double speedKmh);

    Task RemoveScheduledActivityAsync(
        Guid tripId,
        Guid scheduledActivityId);

    Task<ScheduledActivity> UpdateScheduledActivityAsync(
        Guid tripId,
        Guid scheduledActivityId,
        DateTime startTime,
        double speedKmh);
}
