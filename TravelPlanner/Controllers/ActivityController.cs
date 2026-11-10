using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/activities")]
public class ActivityController : ControllerBase
{
    private readonly ActivityManagementService _activityManagementService;

    public ActivityController(ActivityManagementService activityManagementService)
    {
        _activityManagementService = activityManagementService;
    }

    [HttpPost]
    public async Task<IActionResult> AddAsync(
        Guid tripId,
        [FromBody] AddActivityRequest request)
    {
        try
        {
            var activity = await _activityManagementService.AddActivityAsync(
                tripId,
                request.Name,
                request.Category,
                request.Latitude,
                request.Longitude,
                request.DurationHours,
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
        var activities = await _activityManagementService.GetPrioritizedActivitiesAsync(tripId);

        if (activities is null)
        {
            return NotFound("Trip not found.");
        }

        return Ok(activities.Select(ToActivityResponse).ToList());
    }

    [HttpGet("selected")]
    public async Task<IActionResult> GetSelectedActivitiesAsync(
        Guid tripId,
        [FromQuery] decimal? budget,
        [FromQuery] double? hours)
    {
        var (activities, validationError) =
            await _activityManagementService.GetSelectedActivitiesAsync(tripId, budget, hours);

        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        if (activities is null)
        {
            return NotFound("Trip not found.");
        }

        return Ok(activities.Select(ToActivityResponse).ToList());
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
