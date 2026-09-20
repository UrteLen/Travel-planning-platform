namespace TravelPlannerApp.Models;

public readonly struct GeoLocation
{
    public double Latitude { get; }
    public double Longitude { get; }

    public GeoLocation(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude));
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude));
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString()
    {
        return $"{Latitude}, {Longitude}";
    }
}