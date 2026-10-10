using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models.Activities;
using TravelPlanner.Models.Geography;
using TravelPlanner.Models.Planning;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly DayPlanningService _dayPlanning;
        private readonly PlanVersionService _versions;

        public PlanController(TripService tripService, DayPlanningService dayPlanning, PlanVersionService versions)
        {
            _tripService = tripService;
            _dayPlanning = dayPlanning;
            _versions = versions;
        }

        [HttpPost("{tripId}/generate")]
        public IActionResult Generate(Guid tripId, [FromBody] GeneratePlanRequest request)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip is null)
                return NotFound(new { error = "Trip not found" });

            if (request.SpeedKmh <= 0)
                return BadRequest(new { error = "Speed must be positive" });

            GeoLocation start;
            try
            {
                start = new GeoLocation(request.StartLatitude, request.StartLongitude);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(new { error = "Invalid start coordinates" });
            }

            var places = trip.Activities.Select(ToPlace).ToList();

            trip.Plan = _dayPlanning.DayPlan(places, start, request.StartTime, request.SpeedKmh);
            _versions.SaveVersion(trip.Id, trip.Budget, trip.Plan, request.ChangedBy);

            return Ok(trip.Plan.Select(ToResponse).ToList());
        }

        [HttpGet("{tripId}")]
        public IActionResult Get(Guid tripId)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip is null)
                return NotFound(new { error = "Trip not found" });

            return Ok(trip.Plan.Select(ToResponse).ToList());
        }

        [HttpGet("{tripId}/history")]
        public IActionResult History(Guid tripId)
        {
            if (_tripService.FindTripById(tripId) is null)
                return NotFound(new { error = "Trip not found" });

            var history = _versions.GetHistory(tripId).Select(v => new
            {
                v.VersionNumber,
                v.Timestamp,
                v.ChangedBy,
                v.RestoredFromVersion
            }).ToList();

            return Ok(history);
        }

        [HttpPost("{tripId}/restore/{versionNumber}")]
        public IActionResult Restore(Guid tripId, int versionNumber, [FromQuery] string changedBy = "unknown")
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip is null)
                return NotFound(new { error = "Trip not found" });

            try
            {
                var version = _versions.Restore(trip.Id, versionNumber, changedBy);

                // the trip gets its own copies, so editing it never changes the history
                trip.Plan = _versions.CopyPlan(version.PlanSnapshot);
                trip.Budget = version.BudgetSnapshot.Clone();

                return Ok(new
                {
                    version.VersionNumber,
                    version.Timestamp,
                    version.ChangedBy,
                    version.RestoredFromVersion
                });
            }
            catch (ArgumentException)
            {
                return NotFound(new { error = "Version not found" });
            }
        }

        private static Place ToPlace(TripActivity activity)
        {
            return new Place
            {
                Name = activity.Name,
                Location = activity.Location,
                VisitDuration = activity.Duration,
                OpeningTime = activity.OpeningTime ?? TimeSpan.Zero,
                ClosingTime = activity.ClosingTime ?? TimeSpan.Zero   // both zero = open all day
            };
        }

        private static object ToResponse(PlannedVisit visit)
        {
            return new { name = visit.Place.Name, arrivalTime = visit.ArrivalTime, departureTime = visit.DepartureTime };
        }
    }
}