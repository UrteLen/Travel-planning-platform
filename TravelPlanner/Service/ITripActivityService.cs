using TravelPlanner.Models;

namespace TravelPlanner.Service;

public interface ITripActivityService
{
    Task<TripActivity?> AddActivityAsync(
        Guid tripId,
        string name,
        string category,
        double latitude,
        double longitude,
        double durationHours,
        decimal estimatedCost = 0m,
        bool optional = true,
        TimeSpan? openingTime = null,
        TimeSpan? closingTime = null);

    Task<List<TripActivity>?> GetPrioritizedActivitiesAsync(Guid tripId);

    Task<(List<TripActivity>? Activities, string? ValidationError)> GetSelectedActivitiesAsync(
        Guid tripId,
        decimal? budget,
        double? hours);

    Task<List<TripActivity>?> ImportActivitiesAsync(
        Guid tripId,
        Stream stream);
}
