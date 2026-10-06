using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Services;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        public record AddActivityRequest(
            string Name,
            string Category,
            double Latitude,
            double Longitude,
            double DurationHours,
            decimal EstimatedCost = 0m,
            bool Optional = true);

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
