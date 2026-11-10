using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/scheduled-activities")]
public class ScheduleController : ControllerBase
{
    private readonly ActivityManagementService _activityManagementService;

    public ScheduleController(ActivityManagementService activityManagementService)
    {
        _activityManagementService = activityManagementService;
    }

    [HttpPost]
    public async Task<IActionResult> ScheduleActivityAsync(
        Guid tripId,
        [FromBody] ScheduleActivityRequest request)
    {
        if (request.ActivityId == Guid.Empty ||
            request.StartTime == default ||
            !double.IsFinite(request.SpeedKmh) ||
            request.SpeedKmh <= 0)
        {
            return BadRequest("Valid ActivityId, StartTime and positive SpeedKmh are required.");
        }

        try
        {
            var scheduledActivity = await _activityManagementService.ScheduleActivityAsync(
                tripId,
                request.ActivityId,
                DateTime.SpecifyKind(request.StartTime, DateTimeKind.Unspecified),
                request.SpeedKmh);

            return Ok(scheduledActivity);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (OverflowException)
        {
            return BadRequest(
                "Travel time is too large. Check the travel speed.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDailyScheduleAsync(
        Guid tripId,
        [FromQuery] DateTime? date)
    {
        var scheduledActivities = await _activityManagementService.GetScheduleAsync(tripId, date);

        if (scheduledActivities is null)
        {
            return NotFound();
        }

        return Ok(scheduledActivities);
    }

    [HttpDelete("{scheduledActivityId}")]
    public async Task<IActionResult> RemoveScheduledActivityAsync(
        Guid tripId,
        Guid scheduledActivityId)
    {
        try
        {
            await _activityManagementService.RemoveScheduledActivityAsync(
                tripId, scheduledActivityId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{scheduledActivityId}")]
    public async Task<IActionResult> UpdateScheduledActivityAsync(
        Guid tripId,
        Guid scheduledActivityId,
        [FromBody] UpdateScheduledActivityRequest request)
    {
        try
        {
            var scheduledActivity = await _activityManagementService.UpdateScheduledActivityAsync(
                tripId,
                scheduledActivityId,
                DateTime.SpecifyKind(request.StartTime, DateTimeKind.Unspecified),
                request.SpeedKmh);

            return Ok(scheduledActivity);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (OverflowException)
        {
            return BadRequest(
                "Travel time is too large. Check the travel speed.");
        }
    }
}
