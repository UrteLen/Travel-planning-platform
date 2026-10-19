
using System.Collections.Generic;
using System.Linq;
using TravelPlanner.Models.Activities;
using TravelPlanner.Models.Geography;
using TravelPlanner.Models.Planning;
using TravelPlanner.Models.Trips;
using TravelPlanner.Models.History;
using TravelPlanner.Responses;

namespace TravelPlanner.Service
{
    public class PlanService
    {
        private readonly DayPlanningService _dayPlanningService;
        private readonly TripVersionService _versionService;

        public PlanService(
            DayPlanningService dayPlanningService,
            TripVersionService versionService)
        {
            _dayPlanningService = dayPlanningService;
            _versionService = versionService;
        }

        public List<PlannedVisit> GeneratePlan(Trip trip, GeoLocation start, DateTime startTime, double speedKmh, string changedBy)
        {
            var places = trip.Activities.Select(ToPlace).ToList();
            trip.Plan = _dayPlanningService.DayPlan(places, start, startTime, speedKmh);
            _versionService.SaveVersion(trip.Id, trip.Budget, trip.Plan, trip.Activities, changedBy);

            return trip.Plan;
        }

        public TripVersion RestoreVersion(Trip trip, int versionNumber, string changedBy)
        {
            var version = _versionService.Restore(trip.Id, versionNumber, changedBy);

            trip.Plan = _versionService.CopyPlan(version.PlanSnapshot);
            trip.Budget = version.BudgetSnapshot.Clone();
            trip.Activities = _versionService.CopyActivities(version.ActivitiesSnapshot);

            return version;
        }

        public static List<PlannedVisitResponse> ToResponseList(List<PlannedVisit> plan)
        {
            return plan
                .Select(v => new PlannedVisitResponse(v.Place.Name, v.ArrivalTime, v.DepartureTime))
                .ToList();
        }

        private static Place ToPlace(TripActivity activity)
        {
            return new Place
            {
                Name = activity.Name,
                Location = activity.Location,
                VisitDuration = activity.Duration,
                OpeningTime = activity.OpeningTime ?? TimeSpan.Zero,
                ClosingTime = activity.ClosingTime ?? TimeSpan.Zero
            };
        }
    }
}
