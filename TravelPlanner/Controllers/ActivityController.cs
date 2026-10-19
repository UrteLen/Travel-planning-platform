using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Service;
using TravelPlanner.Requests;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/activities")]
public class ActivityController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly ActivityService _activityService;
    private readonly ActivityVotingService _activityVotingService;
    private readonly ActivityImportService _activityImportService;

    public ActivityController(
        TripService tripService,
        ActivityService activityService,
        ActivityVotingService activityVotingService,
        ActivityImportService activityImportService)
    {
        _tripService = tripService;
        _activityService = activityService;
        _activityVotingService = activityVotingService;
        _activityImportService = activityImportService;
    }

    [HttpPost]
    public IActionResult Add(
        Guid tripId,
        [FromBody] AddActivityRequest request)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip is null)
        {
            return NotFound("Trip not found.");
        }

        if (!Enum.TryParse<Category>(request.Category, true, out var category)
            || !Enum.IsDefined(category))
        {
            return BadRequest("Invalid category.");
        }

        if (!double.IsFinite(request.DurationHours) || request.DurationHours <= 0)
        {
            return BadRequest("Activity duration must be a finite number greater than zero.");
        }

        try
        {
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

            return Ok(ToActivityResponse(activity));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (OverflowException)
        {
            return BadRequest("Activity duration is too large.");
        }
    }

    [HttpPost("import")]
    public IActionResult Import(
        Guid tripId,
        IFormFile file)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip is null)
        {
            return NotFound("Trip not found.");
        }

        if (file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        try
        {
            using Stream stream = file.OpenReadStream();

            var activities = _activityImportService.LoadActivitiesFromJson(stream);
            var responses = new List<ActivityResponse>();

            foreach (var activity in activities)
            {
                trip.Activities.Add(activity);
                responses.Add(ToActivityResponse(activity));
            }

            return Ok(responses);
        }
        catch (JsonException)
        {
            return BadRequest("Invalid JSON file.");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (OverflowException)
        {
            return BadRequest("Activity duration is too large.");
        }
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
            responses.Add(ToActivityResponse(activity));
        }

        return Ok(responses);
    }

    [HttpGet("selected")]
    public IActionResult GetSelectedActivities(
        Guid tripId,
        [FromQuery] decimal? budget,
        [FromQuery] double? hours)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip is null)
        {
            return NotFound("Trip not found.");
        }

        if (budget < 0)
        {
            return BadRequest("Budget must be non-negative.");
        }

        if (!hours.HasValue || !double.IsFinite(hours.Value) || hours.Value < 0)
        {
            return BadRequest("Hours are required and must be non-negative and finite.");
        }

        TimeSpan availableTime;
        try
        {
            availableTime = TimeSpan.FromHours(hours.Value);
        }
        catch (OverflowException)
        {
            return BadRequest("Hours value is too large.");
        }

        var activities = _activityVotingService.SelectActivitiesForPlan(trip.Activities, budget, availableTime);
        var responses = new List<ActivityResponse>();

        foreach (var activity in activities)
        {
            responses.Add(ToActivityResponse(activity));
        }

        return Ok(responses);
    }

    private ActivityResponse ToActivityResponse(TripActivity activity)
    {
        return new ActivityResponse(
            activity.Id,
            activity.Name,
            activity.Category,
            new GeoLocationResponse(activity.Location.Latitude, activity.Location.Longitude),
            activity.Duration,
            activity.EstimatedCost,
            activity.IsOptional,
            activity.OpeningTime,
            activity.ClosingTime);
    }
}
