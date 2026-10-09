using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Service;
using TravelPlanner.Requests;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly ActivityService _activityService;
        public ActivityController(TripService tripService, ActivityService activityService)
        {
            this._tripService = tripService;
            this._activityService = activityService;
        }

        [HttpPost("{tripId}")]
        public IActionResult Add(Guid tripId, [FromBody] AddActivityRequest request)
        {
            var trip = _tripService.FindTripById(tripId);

            if (trip is null)
                return NotFound("Trip not found.");

            if (!Enum.TryParse<Category>(request.Category, true, out var category))
                return BadRequest("Invalid category.");

            var location = new GeoLocation(request.Latitude, request.Longitude);
            var duration = TimeSpan.FromHours(request.DurationHours);

            var activity = _activityService.AddActivity(
                trip,
                request.Name,
                category,
                location,
                duration,
                request.EstimatedCost,
                request.Optional);

            return Ok(activity);
        }
    }
}