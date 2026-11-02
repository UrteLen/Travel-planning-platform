using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly PlanService _planService;
    private readonly TripVersionService _versionService;

    public PlanController(
        TripService tripService,
        PlanService planService,
        TripVersionService versionService)
    {
        _tripService = tripService;
        _planService = planService;
        _versionService = versionService;
    }

    [HttpPost("{tripId}/generate")]
    public async Task<IActionResult> GenerateAsync(
        Guid tripId,
        [FromBody] GeneratePlanRequest request)
    {
        var trip = await _tripService.FindTripByIdForUpdateAsync(tripId);

        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        try
        {
            if (request.SpeedKmh <= 0)
            {
                return BadRequest(new { error = "Speed must be positive" });
            }

            GeoLocation start;
            try
            {
                start = new GeoLocation(request.StartLatitude, request.StartLongitude);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(new { error = "Invalid start coordinates" });
            }

            var plan = _planService.GeneratePlan(
                trip,
                start,
                DateTime.SpecifyKind(request.StartTime, DateTimeKind.Unspecified),
                request.SpeedKmh,
                request.ChangedBy);

            await _tripService.SaveChangesAsync();
            return Ok(PlanService.ToResponseList(plan));
        }
        finally
        {
            await _tripService.RollbackTransactionAsync();
        }
    }

    [HttpGet("{tripId}")]
    public async Task<IActionResult> GetAsync(Guid tripId)
    {
        var trip = await _tripService.FindTripByIdAsync(tripId);

        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        return Ok(PlanService.ToResponseList(trip.Plan));
    }

    [HttpGet("{tripId}/history")]
    public async Task<IActionResult> HistoryAsync(Guid tripId)
    {
        if (await _tripService.FindTripByIdAsync(tripId) is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        var history = _versionService.GetHistory(tripId).Select(version => new
        {
            version.VersionNumber,
            version.Timestamp,
            version.ChangedBy,
            version.RestoredFromVersion
        }).ToList();

        return Ok(history);
    }

    [HttpPost("{tripId}/restore/{versionNumber}")]
    public async Task<IActionResult> RestoreAsync(
        Guid tripId,
        int versionNumber,
        [FromQuery] string changedBy = "unknown")
    {
        var trip = await _tripService.FindTripByIdForUpdateAsync(tripId);

        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        try
        {
            var version = _planService.RestoreVersion(trip, versionNumber, changedBy);
            await _tripService.SaveChangesAsync();

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
        finally
        {
            await _tripService.RollbackTransactionAsync();
        }
    }
}
