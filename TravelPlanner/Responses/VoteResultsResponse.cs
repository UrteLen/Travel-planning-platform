using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record VoteResultsResponse(
    int Upvotes,
    int Downvotes,
    VoteType? ParticipantVote);
