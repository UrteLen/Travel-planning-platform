using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/activities")]
public class ActivityController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly ActivityManagementService _activityManagementService;
    private readonly ActivityVotingService _activityVotingService;

    public ActivityController(
        TripService tripService,
        ActivityManagementService activityManagementService,
        ActivityVotingService activityVotingService)
    {
        _tripService = tripService;
        _activityManagementService = activityManagementService;
        _activityVotingService = activityVotingService;
    }

    [HttpPost]
    public async Task<IActionResult> AddAsync(
        Guid tripId,
        [FromBody] AddActivityRequest request)
    {
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

            var activity = await _activityManagementService.AddActivityAsync(
                tripId,
                request.Name,
                category,
                location,
                duration,
                request.EstimatedCost,
                request.Optional,
                request.OpeningTime,
                request.ClosingTime);

            if (activity is null)
            {
                return NotFound("Trip not found.");
            }

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
    public async Task<IActionResult> ImportAsync(
        Guid tripId,
        IFormFile file)
    {
        try
        {
            if (file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            await using Stream stream = file.OpenReadStream();

            var activities = await _activityManagementService
                .ImportActivitiesAsync(tripId, stream);

            if (activities is null)
            {
                return NotFound("Trip not found.");
            }

            var responses = new List<ActivityResponse>();

            foreach (var activity in activities)
            {
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
    public async Task<IActionResult> GetPrioritizedActivitiesAsync(Guid tripId)
    {
        var trip = await _tripService.FindTripByIdAsync(tripId);

        if (trip is null)
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
    public async Task<IActionResult> GetSelectedActivitiesAsync(
        Guid tripId,
        [FromQuery] decimal? budget,
        [FromQuery] double? hours)
    {
        var trip = await _tripService.FindTripByIdAsync(tripId);

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
