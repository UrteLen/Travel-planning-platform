using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class DayPlanningServiceTests
{
    private readonly DayPlanningService _service = new(new VisitScheduler());
    private static readonly DateTime Start = new(2026, 12, 1, 9, 0, 0);
    private static readonly GeoLocation Origin = new(0, 0);

    private static Place CreatePlace(
        string name,
        double longitude,
        double durationHours = 1,
        TimeSpan? opening = null,
        TimeSpan? closing = null) => new()
    {
        Name = name,
        Location = new GeoLocation(0, longitude),
        VisitDuration = TimeSpan.FromHours(durationHours),
        OpeningTime = opening ?? TimeSpan.Zero,
        ClosingTime = closing ?? TimeSpan.Zero
    };

    [Fact]
    public void DayPlan_WithoutPlaces_ReturnsEmptyPlan()
    {
        Assert.Empty(_service.DayPlan(new List<Place>(), Origin, Start, 50));
    }

    [Fact]
    public void DayPlan_VisitsPlacesNearestFirst()
    {
        var places = new List<Place>
        {
            CreatePlace("Far", 0.3),
            CreatePlace("Near", 0.1),
            CreatePlace("Mid", 0.2)
        };

        var plan = _service.DayPlan(places, Origin, Start, 50);

        Assert.Equal(new[] { "Near", "Mid", "Far" }, plan.Select(v => v.Place.Name));
    }

    [Fact]
    public void DayPlan_VisitsDoNotOverlapAndMoveForwardInTime()
    {
        var places = new List<Place>
        {
            CreatePlace("A", 0.1),
            CreatePlace("B", 0.2)
        };

        var plan = _service.DayPlan(places, Origin, Start, 50);

        Assert.Equal(2, plan.Count);
        Assert.True(plan[0].ArrivalTime > Start);
        Assert.True(plan[0].DepartureTime <= plan[1].ArrivalTime);
    }

    [Fact]
    public void DayPlan_SkipsPlacesThatCanNeverBeScheduled()
    {
        var places = new List<Place>
        {
            CreatePlace("Open", 0.1),
            CreatePlace("TooLong", 0.2, durationHours: 2, TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        };

        var plan = _service.DayPlan(places, Origin, Start, 50);

        var visit = Assert.Single(plan);
        Assert.Equal("Open", visit.Place.Name);
    }

    [Fact]
    public void DayPlan_DoesNotChangeTheInputList()
    {
        var places = new List<Place> { CreatePlace("A", 0.1), CreatePlace("B", 0.2) };

        _service.DayPlan(places, Origin, Start, 50);

        Assert.Equal(2, places.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void DayPlan_WithNonPositiveSpeed_Throws(double speed)
    {
        var places = new List<Place> { CreatePlace("A", 0.1) };

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.DayPlan(places, Origin, Start, speed));
    }
}
