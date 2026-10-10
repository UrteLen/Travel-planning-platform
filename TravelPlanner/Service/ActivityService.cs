using TravelPlanner.Models.Trips;
using TravelPlanner.Enums;
using TravelPlanner.Models.Activities;
using TravelPlanner.Models.Geography;

namespace TravelPlanner.Service;

public class ActivityService
{
    public TripActivity AddActivity(
    Trip trip,
    string name,
    Category category,
    GeoLocation location,
    TimeSpan duration,
    decimal estimatedCost = 0m,
    bool optional = true)
    {
        var activity = new TripActivity(
            name,
            category,
            location,
            duration,
            estimatedCost,
            optional);

        trip.Activities.Add(activity);

        return activity;
    }
}