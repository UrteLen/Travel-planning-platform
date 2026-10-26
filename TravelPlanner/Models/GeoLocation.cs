using System.Text.Json.Serialization;

namespace TravelPlanner.Models;

public readonly struct GeoLocation
{
    public double Latitude { get; }
    public double Longitude { get; }

    [JsonConstructor]
    public GeoLocation(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                "Latitude must be a finite number between -90 and 90.");
        }

        if (!double.IsFinite(longitude) || longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                "Longitude must be a finite number between -180 and 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString()
    {
        return $"{Latitude}, {Longitude}";
    }
}
