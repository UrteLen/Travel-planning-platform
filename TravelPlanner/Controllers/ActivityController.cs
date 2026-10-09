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

        [HttpPost("{tripId}")]
        public IActionResult Add(Guid tripId, [FromBody] AddActivityRequest request)
        {
            var trip = TripService.FindTripById(tripId);

            if (trip is null)
                return NotFound("Trip not found.");

            if (!Enum.TryParse<Category>(request.Category, true, out var category))
                return BadRequest("Invalid category.");

            var location = new GeoLocation(request.Latitude, request.Longitude);
            var duration = TimeSpan.FromHours(request.DurationHours);

            var activityService = new ActivityService();

            var activity = activityService.AddActivity(
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