using System.Collections.Generic;
using TravelPlanner.Extensions;
using TravelPlanner.Models.Planning;
using TravelPlanner.Models.Geography;

namespace TravelPlanner.Service
{
    public class DayPlanningService
    {
        private readonly VisitScheduler _scheduler;

        public DayPlanningService(VisitScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public List<PlannedVisit> DayPlan(List<Place> places, GeoLocation startLocation, DateTime startTime, double speedKmh)
        {
            List<PlannedVisit> plan = new();
            List<Place> remaining = new(places);
            GeoLocation currentLocation = startLocation;
            DateTime currentTime = startTime;

            while (remaining.Count > 0)
            {
                Place? best = null;
                DateTime bestStart = default;
                DateTime bestEnd = default;
                double shortest = double.MaxValue;

                foreach (Place candidate in remaining)
                {
                    double distance = currentLocation.DistanceTo(candidate.Location);
                    DateTime arrival = currentTime + TravelTime(distance, speedKmh);

                    if (_scheduler.TrySchedule(arrival, candidate, out DateTime start, out DateTime end)
                        && distance < shortest)
                    {
                        shortest = distance;
                        best = candidate;
                        bestStart = start;
                        bestEnd = end;
                    }
                }

                if (best == null)
                {
                    break;
                }

                plan.Add(new PlannedVisit(best, bestStart, bestEnd));
                currentLocation = best.Location;
                currentTime = bestEnd;
                remaining.Remove(best);
            }

            return plan;
        }

        private TimeSpan TravelTime(double distanceKm, double speedKmh)
        {
            if (speedKmh <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speedKmh), "Speed must be positive.");
            }

            return TimeSpan.FromHours(distanceKm / speedKmh);
        }
    }
}