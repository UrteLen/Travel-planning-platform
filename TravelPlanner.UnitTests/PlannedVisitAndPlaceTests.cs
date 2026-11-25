using TravelPlanner.Models;

namespace TravelPlanner.UnitTests;

public class PlannedVisitAndPlaceTests
{
    private static readonly DateTime Day = new(2026, 12, 1);

    private static Place CreatePlace(string name = "Place") => new()
    {
        Name = name,
        Location = new GeoLocation(41.9, 12.5),
        VisitDuration = TimeSpan.FromHours(1),
        OpeningTime = TimeSpan.FromHours(9),
        ClosingTime = TimeSpan.FromHours(17)
    };

    [Fact]
    public void CompareTo_EarlierArrivalComesFirst()
    {
        var early = new PlannedVisit(CreatePlace(), Day.AddHours(9), Day.AddHours(10));
        var late = new PlannedVisit(CreatePlace(), Day.AddHours(11), Day.AddHours(12));

        Assert.True(early.CompareTo(late) < 0);
        Assert.True(late.CompareTo(early) > 0);
    }

    [Fact]
    public void CompareTo_WhenArrivalIsEqual_ComparesDeparture()
    {
        var short1 = new PlannedVisit(CreatePlace(), Day.AddHours(9), Day.AddHours(10));
        var long1 = new PlannedVisit(CreatePlace(), Day.AddHours(9), Day.AddHours(11));

        Assert.True(short1.CompareTo(long1) < 0);
        Assert.Equal(0, short1.CompareTo(short1));
    }

    [Fact]
    public void CompareTo_Null_IsGreater()
    {
        var visit = new PlannedVisit(CreatePlace(), Day, Day.AddHours(1));

        Assert.Equal(1, visit.CompareTo(null));
    }

    [Fact]
    public void Order_SortsVisitsByArrivalTime()
    {
        var a = new PlannedVisit(CreatePlace("A"), Day.AddHours(14), Day.AddHours(15));
        var b = new PlannedVisit(CreatePlace("B"), Day.AddHours(9), Day.AddHours(10));
        var c = new PlannedVisit(CreatePlace("C"), Day.AddHours(11), Day.AddHours(12));

        var ordered = new List<PlannedVisit> { a, b, c }.Order().Select(v => v.Place.Name);

        Assert.Equal(new[] { "B", "C", "A" }, ordered);
    }

    [Fact]
    public void Place_Clone_CopiesValuesButGetsANewId()
    {
        var original = CreatePlace("Forum");

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.NotEqual(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.VisitDuration, clone.VisitDuration);
        Assert.Equal(original.OpeningTime, clone.OpeningTime);
        Assert.Equal(original.ClosingTime, clone.ClosingTime);
        Assert.Equal(original.Location.Latitude, clone.Location.Latitude);
    }
}
