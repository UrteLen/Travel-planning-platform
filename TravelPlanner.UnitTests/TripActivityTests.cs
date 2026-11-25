using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class TripActivityTests
{
    private static TripActivity Create(
        string name = "Museum",
        TimeSpan? duration = null,
        decimal cost = 0m,
        TimeSpan? opening = null,
        TimeSpan? closing = null) =>
        new(name, Category.Activities, new GeoLocation(41.9, 12.5), duration ?? TimeSpan.FromHours(1), cost, true, opening, closing);

    [Fact]
    public void Constructor_WithValidValues_SetsProperties()
    {
        var activity = Create("Colosseum", TimeSpan.FromHours(2), 25m, TimeSpan.FromHours(9), TimeSpan.FromHours(18));

        Assert.Equal("Colosseum", activity.Name);
        Assert.Equal(Category.Activities, activity.Category);
        Assert.Equal(TimeSpan.FromHours(2), activity.Duration);
        Assert.Equal(25m, activity.EstimatedCost);
        Assert.True(activity.IsOptional);
        Assert.Equal(TimeSpan.FromHours(9), activity.OpeningTime);
        Assert.Equal(TimeSpan.FromHours(18), activity.ClosingTime);
        Assert.Empty(activity.Votes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => Create(name));
    }

    [Fact]
    public void Constructor_WithZeroDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(duration: TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_WithNegativeDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(duration: TimeSpan.FromHours(-1)));
    }

    [Fact]
    public void Constructor_WithNegativeCost_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(cost: -1m));
    }

    [Fact]
    public void Constructor_WithOnlyOpeningTime_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(opening: TimeSpan.FromHours(9)));
    }

    [Fact]
    public void Constructor_WithOnlyClosingTime_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(closing: TimeSpan.FromHours(18)));
    }

    [Fact]
    public void Constructor_WithOpeningTimeOfAFullDay_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(opening: TimeSpan.FromDays(1), closing: TimeSpan.FromHours(10)));
    }

    [Fact]
    public void Constructor_WithNegativeClosingTime_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(opening: TimeSpan.FromHours(9), closing: TimeSpan.FromHours(-1)));
    }

    [Fact]
    public void Clone_CopiesValuesAndKeepsTheSameId()
    {
        var original = Create("Museum", TimeSpan.FromHours(2), 10m, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Duration, clone.Duration);
        Assert.Equal(original.EstimatedCost, clone.EstimatedCost);
        Assert.Equal(original.OpeningTime, clone.OpeningTime);
        Assert.Equal(original.ClosingTime, clone.ClosingTime);
    }

    [Fact]
    public void Clone_CopiesVotesSoChangingTheCloneDoesNotChangeTheOriginal()
    {
        var original = Create();
        original.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var clone = original.Clone();
        clone.Votes[Guid.NewGuid()] = VoteType.Downvote;

        Assert.Single(original.Votes);
        Assert.Equal(2, clone.Votes.Count);
    }

    [Fact]
    public void ActivityService_AddActivity_AddsTheActivityToTheTrip()
    {
        var trip = new Trip();

        var activity = new ActivityService().AddActivity(
            trip, "Pantheon", Category.Activities, new GeoLocation(41.9, 12.4), TimeSpan.FromHours(1));

        Assert.Same(activity, Assert.Single(trip.Activities));
        Assert.Equal("Pantheon", activity.Name);
        Assert.True(activity.IsOptional);
    }

    [Fact]
    public void ActivityService_AddActivity_WithInvalidName_DoesNotAddAnything()
    {
        var trip = new Trip();

        Assert.Throws<ArgumentException>(() => new ActivityService().AddActivity(
            trip, " ", Category.Food, new GeoLocation(0, 0), TimeSpan.FromHours(1)));

        Assert.Empty(trip.Activities);
    }
}
