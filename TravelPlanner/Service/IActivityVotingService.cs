using TravelPlanner.Enums;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public interface IActivityVotingService
{
    Task<(VoteResultsResponse? Results, string? NotFoundError)> GetVoteResultsAsync(
        Guid tripId,
        Guid activityId,
        Guid? participantId);

    Task VoteAsync(
        Guid tripId,
        Guid participantId,
        Guid activityId,
        VoteType voteType);

    Task RemoveVoteAsync(
        Guid tripId,
        Guid participantId,
        Guid activityId);
}
