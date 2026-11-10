using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ActivityVotingServiceTests
{
    [Fact]
    public void SelectActivitiesForPlan_WhenBudgetFitsOnlyOneActivity_SelectsHigherVotedActivity()
    {
        var lowerVotedActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var higherVotedActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        higherVotedActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { lowerVotedActivity, higherVotedActivity },
            12m,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(higherVotedActivity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenTimeFitsOnlyOneActivity_SelectsHigherVotedActivity()
    {
        var lowerVotedActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 0m);

        var higherVotedActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 0m);

        higherVotedActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { lowerVotedActivity, higherVotedActivity },
            null,
            TimeSpan.FromHours(1));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(higherVotedActivity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenBudgetIsNull_IncludesPaidActivity()
    {
        var activity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { activity },
            null,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(activity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenBudgetIsZero_SelectsOnlyFreeActivity()
    {
        var freeActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 0m);

        var paidActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        paidActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { paidActivity, freeActivity },
            0m,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(freeActivity.Id, selectedActivity.Id);
    }
}
