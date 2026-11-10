using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ActivitySchedulingServiceTests
{
    [Fact]
    public void ScheduleActivity_WhenActivitiesOverlap_RejectsNewActivity()
    {
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 1)
        };

        var firstActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var overlappingActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var service = new ActivitySchedulingService();

        service.ScheduleActivity(
            trip,
            firstActivity,
            new DateTime(2026, 12, 1, 10, 0, 0),
            40);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.ScheduleActivity(
                trip,
                overlappingActivity,
                new DateTime(2026, 12, 1, 10, 30, 0),
                40));

        Assert.Contains("time conflict", exception.Message);

        var scheduledActivity = Assert.Single(trip.ScheduledActivities);
        Assert.Equal(firstActivity.Id, scheduledActivity.Activity.Id);
    }

    [Fact]
    public void ScheduleActivity_WhenActivitiesTouchButDoNotOverlap_AllowsBoth()
    {
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 1)
        };

        var firstActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var secondActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var service = new ActivitySchedulingService();

        service.ScheduleActivity(
            trip,
            firstActivity,
            new DateTime(2026, 12, 1, 10, 0, 0),
            40);

        var secondScheduledActivity = service.ScheduleActivity(
            trip,
            secondActivity,
            new DateTime(2026, 12, 1, 11, 0, 0),
            40);

        Assert.Equal(2, trip.ScheduledActivities.Count);
        Assert.Contains(secondScheduledActivity, trip.ScheduledActivities);
    }

    [Fact]
    public void ScheduleActivity_WhenTravelTimeDoesNotFit_RejectsNewActivity()
    {
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 1)
        };

        var firstActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var secondActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2897),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var service = new ActivitySchedulingService();

        service.ScheduleActivity(
            trip,
            firstActivity,
            new DateTime(2026, 12, 1, 10, 0, 0),
            40);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.ScheduleActivity(
                trip,
                secondActivity,
                new DateTime(2026, 12, 1, 11, 0, 0),
                40));

        Assert.Contains("travel time conflict", exception.Message);

        var scheduledActivity = Assert.Single(trip.ScheduledActivities);
        Assert.Equal(firstActivity.Id, scheduledActivity.Activity.Id);
    }

    [Fact]
    public void ScheduleActivity_WhenActivityEndsAfterClosing_RejectsNewActivity()
    {
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 1)
        };

        var activity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m,
            openingTime: TimeSpan.FromHours(9),
            closingTime: TimeSpan.FromHours(17));

        var service = new ActivitySchedulingService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.ScheduleActivity(
                trip,
                activity,
                new DateTime(2026, 12, 1, 16, 30, 0),
                40));

        Assert.Contains("opening hours", exception.Message);

        Assert.Empty(trip.ScheduledActivities);
    }

    [Fact]
    public void ScheduleActivity_WhenActivityEndsAtClosing_AllowsActivity()
    {
        var trip = new Trip
        {
            StartDate = new DateTime(2026, 12, 1),
            EndDate = new DateTime(2026, 12, 1)
        };

        var activity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m,
            openingTime: TimeSpan.FromHours(9),
            closingTime: TimeSpan.FromHours(17));

        var service = new ActivitySchedulingService();

        var scheduledActivity = service.ScheduleActivity(
            trip,
            activity,
            new DateTime(2026, 12, 1, 16, 0, 0),
            40);

        var savedActivity = Assert.Single(trip.ScheduledActivities);
        Assert.Same(scheduledActivity, savedActivity);
        Assert.Equal(activity.Id, savedActivity.Activity.Id);
    }

    [Fact]
    public void GetDailySchedule_WhenActivitySpansMidnight_IncludesItOnBothDays()
    {
        var activity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(2),
            estimatedCost: 12m);

        var scheduledActivity = new ScheduledActivity(
            activity,
            new DateTime(2026, 12, 1, 23, 0, 0));

        var scheduledActivities = new[] { scheduledActivity };
        var service = new ActivitySchedulingService();

        var firstDaySchedule = service.GetDailySchedule(
            scheduledActivities,
            new DateTime(2026, 12, 1));

        var secondDaySchedule = service.GetDailySchedule(
            scheduledActivities,
            new DateTime(2026, 12, 2));

        Assert.Same(scheduledActivity, Assert.Single(firstDaySchedule));
        Assert.Same(scheduledActivity, Assert.Single(secondDaySchedule));
    }
}