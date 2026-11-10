using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/activities/{activityId}/votes")]
public class VotingController : ControllerBase
{
    private readonly ActivityManagementService _activityManagementService;

    public VotingController(ActivityManagementService activityManagementService)
    {
        _activityManagementService = activityManagementService;
    }

    [HttpPut("{participantId}")]
    public async Task<IActionResult> VoteAsync(
        Guid tripId,
        Guid activityId,
        Guid participantId,
        [FromBody] VoteRequest request)
    {
        if (request.VoteType is null)
        {
            return BadRequest("Vote type is required.");
        }

        try
        {
            await _activityManagementService.VoteAsync(
                tripId, participantId, activityId, request.VoteType.Value);

            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetVoteResultsAsync(
        Guid tripId,
        Guid activityId,
        [FromQuery] Guid? participantId = null)
    {
        var (results, notFoundError) =
            await _activityManagementService.GetVoteResultsAsync(
                tripId,
                activityId,
                participantId);

        if (notFoundError is not null)
        {
            return NotFound(notFoundError);
        }

        return Ok(results);
    }

    [HttpDelete("{participantId}")]
    public async Task<IActionResult> RemoveVoteAsync(
        Guid tripId,
        Guid activityId,
        Guid participantId)
    {
        try
        {
            await _activityManagementService.RemoveVoteAsync(
                tripId, participantId, activityId);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
