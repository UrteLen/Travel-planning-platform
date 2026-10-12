using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Service;
using TravelPlanner.Requests;
using TravelPlanner.Enums;
using TravelPlanner.Models.Geography;
using TravelPlanner.Responses;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/trips/{tripId}/activities")]
    public class ActivityController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly ActivityService _activityService;
        private readonly ActivityVotingService _activityVotingService;

        public ActivityController(TripService tripService, ActivityService activityService,
            ActivityVotingService activityVotingService)
        {
            this._tripService = tripService;
            this._activityService = activityService;
            this._activityVotingService = activityVotingService;
        }

        [HttpPost]
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
                request.Optional,
                request.OpeningTime,
                request.ClosingTime);

            var response = new ActivityResponse(
                activity.Id,
                activity.Name,
                activity.Category,
                new GeoLocationResponse(activity.Location.Latitude, activity.Location.Longitude),
                activity.Duration,
                activity.EstimatedCost,
                activity.IsOptional,
                activity.OpeningTime,
                activity.ClosingTime
            );

            return Ok(response);
        }

        [HttpGet("prioritized")]
        public IActionResult GetPrioritizedActivities(Guid tripId)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip == null)
            {
                return NotFound("Trip not found.");
            }

            var activities = _activityVotingService.PrioritizeByVotes(trip.Activities);
            var responses = new List<ActivityResponse>();
            
            foreach (var activity in activities)
            {
                responses.Add(new ActivityResponse(
                    activity.Id,
                    activity.Name,
                    activity.Category,
                    new GeoLocationResponse(activity.Location.Latitude, activity.Location.Longitude),
                    activity.Duration,
                    activity.EstimatedCost,
                    activity.IsOptional,
                    activity.OpeningTime,
                    activity.ClosingTime
                ));
            }

            return Ok(responses);
        }
    }
}