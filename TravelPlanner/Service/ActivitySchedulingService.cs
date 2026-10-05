using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class ActivitySchedulingService
{
    public bool HasTimeConflict(ScheduledActivity first, ScheduledActivity second)
    {
        return first.StartTime < second.EndTime && second.StartTime < first.EndTime;
    }

    public bool HasTravelTimeConflict(ScheduledActivity first, ScheduledActivity second, TimeSpan travelTime)
    {
        return first.EndTime + travelTime > second.StartTime;
    }

    public bool HasOpeningHoursConflict(ScheduledActivity scheduled)
    {
        var openingTime = scheduled.Activity.OpeningTime;
        var closingTime = scheduled.Activity.ClosingTime;

        if (openingTime == null || closingTime == null)
        {
            return false;
        }

        TimeSpan startTime = scheduled.StartTime.TimeOfDay;
        TimeSpan endTime = scheduled.EndTime.TimeOfDay;

        return startTime < openingTime || endTime > closingTime;
    }

    public List<ScheduledActivity> GetDailySchedule(IEnumerable<ScheduledActivity> activities, DateTime date)
    {
        return activities
            .Where(activity => activity.StartTime.Date == date.Date)
            .OrderBy(activity => activity.StartTime)
            .ToList();
    }
}
