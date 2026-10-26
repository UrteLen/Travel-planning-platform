using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/scheduled-activities")]
public class ScheduleController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly ActivitySchedulingService _activitySchedulingService;

    public ScheduleController(
        TripService tripService,
        ActivitySchedulingService activitySchedulingService)
    {
        _tripService = tripService;
        _activitySchedulingService = activitySchedulingService;
    }

    [HttpPost]
    public IActionResult ScheduleActivity(
        Guid tripId,
        [FromBody] ScheduleActivityRequest request)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound();
        }

        if (request.ActivityId == Guid.Empty ||
            request.StartTime == default ||
            !double.IsFinite(request.SpeedKmh) ||
            request.SpeedKmh <= 0)
        {
            return BadRequest("Valid ActivityId, StartTime and positive SpeedKmh are required.");
        }

        var activity = trip.Activities
            .FirstOrDefault(candidate => candidate.Id == request.ActivityId);

        if (activity == null)
        {
            return NotFound();
        }

        try
        {
            var scheduledActivity = _activitySchedulingService.ScheduleActivity(
                trip,
                activity,
                DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
                request.SpeedKmh);

            _tripService.SaveChanges();
            return Ok(scheduledActivity);
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
    public IActionResult GetDailySchedule(
        Guid tripId,
        [FromQuery] DateTime? date)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound();
        }

        var scheduledActivities = trip.ScheduledActivities;

        if (date.HasValue)
        {
            scheduledActivities = _activitySchedulingService.GetDailySchedule(
                scheduledActivities, date.Value);
        }

        return Ok(scheduledActivities);
    }

    [HttpDelete("{scheduledActivityId}")]
    public IActionResult RemoveScheduledActivity(
        Guid tripId,
        Guid scheduledActivityId)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        if (!_activitySchedulingService.RemoveScheduledActivity(trip, scheduledActivityId))
        {
            return NotFound("Scheduled activity not found.");
        }

        _tripService.SaveChanges();
        return NoContent();
    }

    [HttpPut("{scheduledActivityId}")]
    public IActionResult UpdateScheduledActivity(
        Guid tripId,
        Guid scheduledActivityId,
        [FromBody] UpdateScheduledActivityRequest request)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        try
        {
            var updatedActivity = _activitySchedulingService.UpdateScheduledActivity(
                trip,
                scheduledActivityId,
                DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
                request.SpeedKmh);

            _tripService.SaveChanges();
            return Ok(updatedActivity);
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