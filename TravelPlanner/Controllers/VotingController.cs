using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Enums;
using TravelPlanner.Requests;
using TravelPlanner.Service;
using TravelPlanner.Responses;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/trips/{tripId}/activities/{activityId}/votes")]
public class VotingController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly ActivityVotingService _activityVotingService;

    public VotingController(
        TripService tripService,
        ActivityVotingService activityVotingService)
    {
        _tripService = tripService;
        _activityVotingService = activityVotingService;
    }

    [HttpPut("{participantId}")]
    public IActionResult Vote(
        Guid tripId,
        Guid activityId,
        Guid participantId,
        [FromBody] VoteRequest request)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        try
        {
            if (request.VoteType is null)
            {
                return BadRequest("Vote type is required.");
            }

            _activityVotingService.Vote(
                trip,
                participantId,
                activityId,
                request.VoteType.Value);

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
    public IActionResult GetVoteResults(
        Guid tripId,
        Guid activityId,
        [FromQuery] Guid? participantId = null)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        var activity = trip.Activities
            .FirstOrDefault(candidate => candidate.Id == activityId);

        if (activity == null)
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
    public IActionResult RemoveVote(
        Guid tripId,
        Guid activityId,
        Guid participantId)
    {
        var trip = _tripService.FindTripById(tripId);

        if (trip == null)
        {
            return NotFound("Trip not found.");
        }

        try
        {
            if (!_activityVotingService.RemoveVote(trip, participantId, activityId))
            {
                return NotFound("Vote not found.");
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }

        return NoContent();
    }
}
