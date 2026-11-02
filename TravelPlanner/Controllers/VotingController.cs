using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Enums;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/activities/{activityId}/votes")]
public class VotingController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly ActivityVotingService _activityVotingService;
    private readonly ActivityManagementService _activityManagementService;

    public VotingController(
        TripService tripService,
        ActivityVotingService activityVotingService,
        ActivityManagementService activityManagementService)
    {
        _tripService = tripService;
        _activityVotingService = activityVotingService;
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
        var trip = await _tripService.FindTripByIdAsync(tripId);

        if (trip is null)
        {
            return NotFound("Trip not found.");
        }

        var activity = trip.Activities
            .FirstOrDefault(candidate => candidate.Id == activityId);

        if (activity is null)
        {
            return NotFound("Activity not found in the trip.");
        }

        var upvotes = _activityVotingService.GetUpvoteCount(activity);
        var downvotes = _activityVotingService.GetDownvoteCount(activity);

        VoteType? participantVote = null;

        if (participantId.HasValue &&
            activity.Votes.TryGetValue(participantId.Value, out var existingVote))
        {
            participantVote = existingVote;
        }

        var results = new VoteResultsResponse(
            upvotes,
            downvotes,
            participantVote);

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
