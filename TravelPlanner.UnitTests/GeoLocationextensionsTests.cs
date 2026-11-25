using TravelPlanner.Extensions;
using TravelPlanner.Models;

namespace TravelPlanner.UnitTests;

public class GeoLocationExtensionsTests
{
    [Fact]
    public void DistanceTo_SamePoint_IsZero()
    {
        var point = new GeoLocation(54.6872, 25.2797);

        Assert.Equal(0d, point.DistanceTo(point), 6);
    }

    [Fact]
    public void DistanceTo_OneDegreeOfLongitudeAtTheEquator_IsAbout111Km()
    {
        var distance = new GeoLocation(0, 0).DistanceTo(new GeoLocation(0, 1));

        Assert.InRange(distance, 111.0, 111.4);
    }

    [Fact]
    public void DistanceTo_VilniusToKaunas_IsAbout90Kilometers()
    {
        var vilnius = new GeoLocation(54.6872, 25.2797);
        var kaunas = new GeoLocation(54.8985, 23.9036);

        Assert.InRange(vilnius.DistanceTo(kaunas), 90.0, 96.0);
    }

    [Fact]
    public void DistanceTo_IsSymmetric()
    {
        var a = new GeoLocation(41.9028, 12.4964);
        var b = new GeoLocation(48.8566, 2.3522);

        Assert.Equal(a.DistanceTo(b), b.DistanceTo(a), 6);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void GeoLocation_WithInvalidCoordinates_Throws(double latitude, double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(latitude, longitude));
    }

    [Fact]
    public void GeoLocation_KeepsValidCoordinates()
    {
        var location = new GeoLocation(41.9, 12.5);

        Assert.Equal(41.9, location.Latitude);
        Assert.Equal(12.5, location.Longitude);
        Assert.Contains(", ", location.ToString());
    }
}
