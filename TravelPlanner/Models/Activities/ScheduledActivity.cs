namespace TravelPlanner.Models.Activities;

public class ScheduledActivity
{
    public TripActivity Activity { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime => StartTime + Activity.Duration;
    public ScheduledActivity(TripActivity activity, DateTime startTime)
    {
        Activity = activity;
        StartTime = startTime;
    }
}
