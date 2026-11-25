using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ActivityServiceTests
{
    [Fact]
    public void AddActivity_WhenValid_AddsActivityWithItsOvernightHours()
    {
        var trip = new Trip();
        var service = new ActivityService();
        var opening = TimeSpan.FromHours(22);
        var closing = TimeSpan.FromHours(2);

        var activity = service.AddActivity(
            trip,
            "Night tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m,
            optional: false,
            openingTime: opening,
            closingTime: closing);

        Assert.Same(activity, Assert.Single(trip.Activities));
        Assert.Equal(opening, activity.OpeningTime);
        Assert.Equal(closing, activity.ClosingTime);
        Assert.Equal(12m, activity.EstimatedCost);
        Assert.False(activity.IsOptional);
    }

    [Theory]
    [InlineData("", 60, 0, "name")]
    [InlineData("  ", 60, 0, "name")]
    [InlineData("Museum", 0, 0, "duration")]
    [InlineData("Museum", 60, -1, "estimatedCost")]
    public void AddActivity_WhenDetailsAreInvalid_LeavesTripUnchanged(
        string name,
        int minutes,
        int cost,
        string expectedParameter)
    {
        var trip = new Trip();
        var service = new ActivityService();

        var exception = Assert.Throws<ArgumentException>(() => service.AddActivity(
            trip,
            name,
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromMinutes(minutes),
            estimatedCost: cost));

        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Empty(trip.Activities);
    }

    [Theory]
    [InlineData(9, null, null)]
    [InlineData(null, 17, null)]
    [InlineData(-1, 17, "openingTime")]
    [InlineData(24, 17, "openingTime")]
    [InlineData(9, -1, "closingTime")]
    [InlineData(9, 24, "closingTime")]
    public void AddActivity_WhenOpeningHoursAreInvalid_LeavesTripUnchanged(
        int? openingHour,
        int? closingHour,
        string? expectedParameter)
    {
        var trip = new Trip();
        var service = new ActivityService();
        TimeSpan? opening = openingHour is int start ? TimeSpan.FromHours(start) : null;
        TimeSpan? closing = closingHour is int end ? TimeSpan.FromHours(end) : null;

        var exception = Assert.Throws<ArgumentException>(() => service.AddActivity(
            trip,
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            openingTime: opening,
            closingTime: closing));

        Assert.Equal(expectedParameter, exception.ParamName);
        if (expectedParameter is null)
        {
            Assert.Equal("Provide both opening and closing times, or neither.", exception.Message);
        }

        Assert.Empty(trip.Activities);
    }
}
