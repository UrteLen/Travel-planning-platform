using System;
using TravelPlanner.Models.Planning;
using TravelPlanner.Models.Geography;

namespace TravelPlanner.Extensions
{
    public static class GeoLocationExtensions
    {
        public static double DistanceTo(this GeoLocation location1, GeoLocation location2)
        {
            //Haversine formula
            const double earthRadiusKm = 6371.0;

            double lat1Rad = location1.Latitude * Math.PI / 180.0;
            double lat2Rad = location2.Latitude * Math.PI / 180.0;
            double deltaLat = (location2.Latitude - location1.Latitude) * Math.PI / 180.0;
            double deltaLon = (location2.Longitude - location1.Longitude) * Math.PI / 180.0;

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusKm * c;
        }
        public static TimeSpan TimeTo(this double distanceKm, double speedKmh)
        {
            return TimeSpan.FromHours(distanceKm / speedKmh);
        }
        public static bool CanVisit (DateTime currentTime, Place place, TimeSpan travelTime)
        {
            DateTime arrivalTime = currentTime + travelTime;
            DateTime finishTime = arrivalTime + place.VisitDuration;
            return arrivalTime.TimeOfDay >= place.OpeningTime && finishTime.TimeOfDay <= place.ClosingTime;
        }
    }
}