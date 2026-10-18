using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly PlanService _planService;
        private readonly TripVersionService _versionService;

        public PlanController(TripService tripService, PlanService planService, TripVersionService versionService)
        {
            _tripService = tripService;
            _planService = planService;
            _versionService = versionService;
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

            var plan = _planService.GeneratePlan(trip, start, request.StartTime, request.SpeedKmh, request.ChangedBy);

            return Ok(PlanService.ToResponseList(plan));
        }

        [HttpGet("{tripId}")]
        public IActionResult Get(Guid tripId)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip is null)
                return NotFound(new { error = "Trip not found" });

            return Ok(PlanService.ToResponseList(trip.Plan));
        }

        [HttpGet("{tripId}/history")]
        public IActionResult History(Guid tripId)
        {
            if (_tripService.FindTripById(tripId) is null)
                return NotFound(new { error = "Trip not found" });

            var history = _versionService.GetHistory(tripId).Select(v => new
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
                var version = _planService.RestoreVersion(trip, versionNumber, changedBy);

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
    }
}