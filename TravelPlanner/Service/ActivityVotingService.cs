using TravelPlanner.Models.Activities;

namespace TravelPlanner.Service;

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
    public List<TripActivity> SelectActivitiesForPlan(IEnumerable<TripActivity> activities, decimal availableBudget, TimeSpan availableTime)
    {
        var prioritizedActivities = PrioritizeByVotes(activities);
        var selectedActivities = new List<TripActivity>();

        decimal usedBudget = 0m;
        TimeSpan usedTime = TimeSpan.Zero;

        foreach (var activity in prioritizedActivities)
        {
            bool fitsBudget = usedBudget + activity.EstimatedCost <= availableBudget;
            bool fitsTime = usedTime + activity.Duration <= availableTime;

            if (fitsBudget && fitsTime)
            {
                selectedActivities.Add(activity);

                usedBudget += activity.EstimatedCost;
                usedTime += activity.Duration;
            }
        }
        return selectedActivities;
    }
}