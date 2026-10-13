using TravelPlanner.Extensions;
using TravelPlanner.Models.Activities;
using TravelPlanner.Models.Trips;

namespace TravelPlanner.Service;

public class ActivitySchedulingService
{
    public bool HasTimeConflict(ScheduledActivity first, ScheduledActivity second)
    {
        return first.StartTime < second.EndTime && second.StartTime < first.EndTime;
    }

    public bool HasTravelTimeConflict(
        ScheduledActivity first,
        ScheduledActivity second,
        TimeSpan travelTime)
    {
        var earlier = first.StartTime <= second.StartTime ? first : second;
        var later = first.StartTime <= second.StartTime ? second : first;

        return earlier.EndTime + travelTime > later.StartTime;
    }

    public bool HasOpeningHoursConflict(ScheduledActivity scheduled)
    {
        var openingTime = scheduled.Activity.OpeningTime;
        var closingTime = scheduled.Activity.ClosingTime;

        if (openingTime == null || closingTime == null)
            return false;

        DateTime openingDateTime;
        DateTime closingDateTime;

        if (closingTime.Value > openingTime.Value)
        {
            // Opens and closes on the same day
            openingDateTime = scheduled.StartTime.Date + openingTime.Value;
            closingDateTime = scheduled.StartTime.Date + closingTime.Value;
        }
        else
        {
            // Closing time is after midnight
            if (scheduled.StartTime.TimeOfDay >= openingTime.Value)
            {
                openingDateTime = scheduled.StartTime.Date + openingTime.Value;
                closingDateTime = scheduled.StartTime.Date.AddDays(1) + closingTime.Value;
            }
            else
            {
                openingDateTime = scheduled.StartTime.Date.AddDays(-1) + openingTime.Value;
                closingDateTime = scheduled.StartTime.Date + closingTime.Value;
            }
        }

        return scheduled.StartTime < openingDateTime ||
               scheduled.EndTime > closingDateTime;
    }

    public List<ScheduledActivity> GetDailySchedule(
        IEnumerable<ScheduledActivity> activities,
        DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        return activities
            .Where(activity =>
                activity.StartTime < dayEnd &&
                activity.EndTime > dayStart)
            .OrderBy(activity => activity.StartTime)
            .ToList();
    }

    private void ValidateSchedule(
        Trip trip,
        ScheduledActivity scheduledActivity,
        double speedKmh,
        ScheduledActivity? ignoredActivity = null)
    {
        var otherActivities = trip.ScheduledActivities
            .Where(x => x != ignoredActivity)
            .ToList();
        
        if (speedKmh <= 0 || !double.IsFinite(speedKmh))
        {
            throw new ArgumentException(
                "Speed must be a positive, finite number.",
                nameof(speedKmh));
        }
        
        if (scheduledActivity.StartTime.Date < trip.StartDate.Date)
        {
            throw new InvalidOperationException("Activity cannot be scheduled before the trip starts.");
        }

        if (scheduledActivity.EndTime > trip.EndDate.Date.AddDays(1))
        {
            throw new InvalidOperationException("Activity cannot be scheduled after the trip ends.");
        }

        if (HasOpeningHoursConflict(scheduledActivity))
        {
            throw new InvalidOperationException("Activity cannot be scheduled outside of its opening hours.");
        }

        foreach (ScheduledActivity existingActivity in otherActivities)
        {
            if (HasTimeConflict(scheduledActivity, existingActivity))
            {
                throw new InvalidOperationException("Activity cannot be scheduled due to a time conflict with another activity.");
            }
        }

        var previousActivity = otherActivities
            .Where(x => x.StartTime < scheduledActivity.StartTime)
            .OrderByDescending(x => x.StartTime)
            .FirstOrDefault();
        var nextActivity = otherActivities
            .Where(x => x.StartTime > scheduledActivity.StartTime)
            .OrderBy(x => x.StartTime)
            .FirstOrDefault();

        if (previousActivity != null)
        {
            double distance = scheduledActivity.Activity.Location.DistanceTo(previousActivity.Activity.Location);
            TimeSpan travelTime = TimeSpan.FromHours(distance / speedKmh);
            if (HasTravelTimeConflict(scheduledActivity, previousActivity, travelTime))
            {
                throw new InvalidOperationException("Activity cannot be scheduled due to a travel time conflict with another activity.");
            }
        }

        if (nextActivity != null)
        {
            double distance = scheduledActivity.Activity.Location.DistanceTo(nextActivity.Activity.Location);
            TimeSpan travelTime = TimeSpan.FromHours(distance / speedKmh);
            if (HasTravelTimeConflict(scheduledActivity, nextActivity, travelTime))
            {
                throw new InvalidOperationException("Activity cannot be scheduled due to a travel time conflict with another activity.");
            }
        }
    }

    public ScheduledActivity ScheduleActivity(Trip trip, TripActivity activity,
        DateTime startTime, double speedKmh)
    {
        var scheduledActivity = new ScheduledActivity(activity, startTime);

        ValidateSchedule(trip, scheduledActivity, speedKmh);

        trip.ScheduledActivities.Add(scheduledActivity);

        return scheduledActivity;
    }

    public bool RemoveScheduledActivity(Trip trip, Guid scheduledActivityId)
    {
        var scheduledActivity = trip.ScheduledActivities
            .FirstOrDefault(x => x.Id == scheduledActivityId);
        if (scheduledActivity == null)
        {
            return false;
        }

        return trip.ScheduledActivities.Remove(scheduledActivity);
    }

    public ScheduledActivity UpdateScheduledActivity(
        Trip trip,
        Guid scheduledActivityId,
        DateTime newStartTime,
        double speedKmh)
    {
        var scheduledActivity = trip.ScheduledActivities
            .FirstOrDefault(x => x.Id == scheduledActivityId);

        if (scheduledActivity == null)
        {
            throw new KeyNotFoundException("Scheduled activity not found.");
        }

        var candidate = new ScheduledActivity(scheduledActivity.Activity, newStartTime);
        ValidateSchedule(trip, candidate, speedKmh, scheduledActivity);
        scheduledActivity.StartTime = newStartTime;

        return scheduledActivity;
    }
}
