using TravelPlanner.Enums;
using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class ActivityVotingService
{
    public void Vote(
        Trip trip,
        Guid participantId,
        Guid activityId,
        VoteType voteType)
    {
        if (!Enum.IsDefined(voteType))
        {
            throw new ArgumentException("Invalid vote type.");
        }

        if (!trip.Participants.ContainsKey(participantId))
        {
            throw new KeyNotFoundException("Participant not found in this trip.");
        }

        var activity = trip.Activities
            .FirstOrDefault(x => x.Id == activityId);

        if (activity == null)
        {
            throw new KeyNotFoundException("Activity not found in the trip.");
        }

        activity.Votes[participantId] = voteType;
    }

    public int GetUpvoteCount(TripActivity activity)
    {
        return activity.Votes.Values.Count(vote => vote == VoteType.Upvote);
    }

    public int GetDownvoteCount(TripActivity activity)
    {
        return activity.Votes.Values.Count(vote => vote == VoteType.Downvote);
    }

    public bool RemoveVote(Trip trip, Guid participantId, Guid activityId)
    {
        if (!trip.Participants.ContainsKey(participantId))
        {
            throw new KeyNotFoundException("Participant not found in this trip.");
        }

        var activity = trip.Activities
            .FirstOrDefault(x => x.Id == activityId);

        if (activity == null)
        {
            throw new KeyNotFoundException("Activity not found in the trip.");
        }

        return activity.Votes.Remove(participantId);
    }

    public List<TripActivity> PrioritizeByVotes(IEnumerable<TripActivity> activities)
    {
        return activities
            .OrderByDescending(activity => GetUpvoteCount(activity) - GetDownvoteCount(activity))
            .ThenByDescending(activity => GetUpvoteCount(activity))
            .ToList();
    }

    public List<TripActivity> SelectActivitiesForPlan(
        IEnumerable<TripActivity> activities,
        decimal availableBudget,
        TimeSpan availableTime)
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
