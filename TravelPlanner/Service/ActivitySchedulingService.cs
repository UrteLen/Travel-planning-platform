using TravelPlanner.Models;

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
}
