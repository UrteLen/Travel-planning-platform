using TravelPlannerApp.Models;

namespace TravelPlannerApp.Service;

public class ActivityVotingService
{
    private readonly Dictionary<Guid, HashSet<Guid>> _votes = new();
    public bool AddVote(Guid activityId, Guid participantId)
    {
        if (!_votes.ContainsKey(activityId))
        {
            _votes[activityId] = new HashSet<Guid>();
        }

        return _votes[activityId].Add(participantId);
    }
    public bool RemoveVote(Guid activityId, Guid participantId)
    {
        if (!_votes.ContainsKey(activityId))
        {
            return false;
        }

        return _votes[activityId].Remove(participantId);
    }
    public int GetVoteCount(Guid activityId)
    {
        if (!_votes.ContainsKey(activityId))
        {
            return 0;
        }

        return _votes[activityId].Count;
    }
    public List<TripActivity> PrioritizeByVotes(IEnumerable<TripActivity> activities)
    {
        return activities
            .OrderByDescending(activity => GetVoteCount(activity.Id))
            .ToList();
    }
}