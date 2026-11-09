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
        var (plan, validationError) = await _planService.GeneratePlanAsync(
            tripId,
            request.StartLatitude,
            request.StartLongitude,
            request.StartTime,
            request.SpeedKmh,
            request.ChangedBy);

        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        if (plan is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        return Ok(PlanService.ToResponseList(plan));
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

        var versions = await _versionService.GetHistoryAsync(tripId);
        var history = versions.Select(version => new
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
            var version = await _planService.RestoreVersionAsync(trip, versionNumber, changedBy);
            await _tripService.SaveChangesAsync();

            return Ok(new
            {
                version.VersionNumber,
                version.Timestamp,
                version.ChangedBy,
                version.RestoredFromVersion
            });
        }
        finally
        {
            await _tripService.RollbackTransactionAsync();
        }
    }
}
