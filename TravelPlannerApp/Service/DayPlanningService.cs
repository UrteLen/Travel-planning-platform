using System.Collections.Generic;
using TravelPlannerApp.Models;
using TravelPlannerApp.Extensions;

namespace TravelPlannerApp.Service
{
    public static class DayPlanningService
    {
        public static List<PlannedVisit> DayPlan(List<Place> places, GeoLocation startLocation, DateTime startTime, double speedKmh)
        {
            List<PlannedVisit> plan = new();
            List<Place> remainingPlaces = new(places);
            GeoLocation currentLocation = startLocation;
            DateTime currentTime = startTime;

            while (remainingPlaces.Count > 0)
            {
                Place bestPlace = null;
                double shortestDistance = double.MaxValue;

                foreach (Place candidate in remainingPlaces)
                {
                    double newDistance = currentLocation.DistanceTo(candidate.Location);
                    TimeSpan newTime = newDistance.TimeTo(speedKmh);
                    if (GeoLocationExtensions.CanVisit(currentTime, candidate, newTime))
                    {
                        if (newDistance < shortestDistance)
                        {
                            shortestDistance = newDistance;
                            bestPlace = candidate;
                        }
                    }

                }
                if (bestPlace == null)
                {
                    break;
                } 
                double bestDistance = currentLocation.DistanceTo(bestPlace.Location);
                TimeSpan travelTime = bestDistance.TimeTo(speedKmh);
                DateTime arrivalTime = currentTime + travelTime;
                DateTime departureTime = arrivalTime + bestPlace.VisitDuration;

                plan.Add(new PlannedVisit(best, arrivalTime, departureTime));
                currentLocation = best.Location;
                currentTime = departureTime;
                remainingPlaces.Remove(bestPlace);

            }
            return plan;
        }
    }
}
