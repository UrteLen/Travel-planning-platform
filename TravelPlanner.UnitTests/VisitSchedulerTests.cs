using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class VisitSchedulerTests
{
    private readonly VisitScheduler _scheduler = new();
    private static readonly DateTime Day = new(2026, 12, 1);

    private static Place CreatePlace(double durationHours, TimeSpan opening, TimeSpan closing) => new()
    {
        Name = "Place",
        Location = new GeoLocation(0, 0),
        VisitDuration = TimeSpan.FromHours(durationHours),
        OpeningTime = opening,
        ClosingTime = closing
    };

    [Fact]
    public void TrySchedule_WhenOpenAllDay_VisitsImmediately()
    {
        var place = CreatePlace(2, TimeSpan.Zero, TimeSpan.Zero);
        var arrival = Day.AddHours(3);

        var ok = _scheduler.TrySchedule(arrival, place, out var start, out var end);

        Assert.True(ok);
        Assert.Equal(arrival, start);
        Assert.Equal(arrival.AddHours(2), end);
    }

    [Fact]
    public void TrySchedule_WhenArrivingInsideOpeningHours_StartsOnArrival()
    {
        var place = CreatePlace(1, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var ok = _scheduler.TrySchedule(Day.AddHours(10), place, out var start, out var end);

        Assert.True(ok);
        Assert.Equal(Day.AddHours(10), start);
        Assert.Equal(Day.AddHours(11), end);
    }

    [Fact]
    public void TrySchedule_WhenArrivingBeforeOpening_WaitsUntilOpening()
    {
        var place = CreatePlace(1, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var ok = _scheduler.TrySchedule(Day.AddHours(7), place, out var start, out var end);

        Assert.True(ok);
        Assert.Equal(Day.AddHours(9), start);
        Assert.Equal(Day.AddHours(10), end);
    }

    [Fact]
    public void TrySchedule_WhenVisitWouldEndAfterClosing_ReturnsFalse()
    {
        var place = CreatePlace(1, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var ok = _scheduler.TrySchedule(Day.AddHours(16.5), place, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TrySchedule_WhenArrivingAfterClosing_ReturnsFalse()
    {
        var place = CreatePlace(1, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var ok = _scheduler.TrySchedule(Day.AddHours(18), place, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TrySchedule_WhenPlaceClosesAfterMidnight_AllowsEveningArrival()
    {
        var place = CreatePlace(1, TimeSpan.FromHours(22), TimeSpan.FromHours(2));

        var ok = _scheduler.TrySchedule(Day.AddHours(23), place, out var start, out var end);

        Assert.True(ok);
        Assert.Equal(Day.AddHours(23), start);
        Assert.Equal(Day.AddDays(1), end);
    }

    [Fact]
    public void TrySchedule_WhenPlaceClosesAfterMidnight_AllowsEarlyMorningArrival()
    {
        var place = CreatePlace(1, TimeSpan.FromHours(22), TimeSpan.FromHours(2));

        var ok = _scheduler.TrySchedule(Day.AddHours(1), place, out var start, out var end);

        Assert.True(ok);
        Assert.Equal(Day.AddHours(1), start);
        Assert.Equal(Day.AddHours(2), end);
    }
}
