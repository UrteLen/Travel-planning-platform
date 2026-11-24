using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ActivityVotingServiceTests
{
    [Fact]
    public void SelectActivitiesForPlan_WhenBudgetFitsOnlyOneActivity_SelectsHigherVotedActivity()
    {
        var lowerVotedActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var higherVotedActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        higherVotedActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { lowerVotedActivity, higherVotedActivity },
            12m,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(higherVotedActivity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenTimeFitsOnlyOneActivity_SelectsHigherVotedActivity()
    {
        var lowerVotedActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 0m);

        var higherVotedActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 0m);

        higherVotedActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { lowerVotedActivity, higherVotedActivity },
            null,
            TimeSpan.FromHours(1));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(higherVotedActivity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenBudgetIsNull_IncludesPaidActivity()
    {
        var activity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { activity },
            null,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(activity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenBudgetIsZero_SelectsOnlyFreeActivity()
    {
        var freeActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 0m);

        var paidActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        paidActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { paidActivity, freeActivity },
            0m,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(freeActivity.Id, selectedActivity.Id);
    }

    [Fact]
    public void SelectActivitiesForPlan_WhenVoteScoresDiffer_SelectsHigherNetScore()
    {
        var lowerScoreActivity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        var higherScoreActivity = new TripActivity(
            "City tour",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);

        lowerScoreActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;
        lowerScoreActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;
        lowerScoreActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;
        lowerScoreActivity.Votes[Guid.NewGuid()] = VoteType.Downvote;
        lowerScoreActivity.Votes[Guid.NewGuid()] = VoteType.Downvote;

        higherScoreActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;
        higherScoreActivity.Votes[Guid.NewGuid()] = VoteType.Upvote;

        var service = new ActivityVotingService();

        var selectedActivities = service.SelectActivitiesForPlan(
            new[] { lowerScoreActivity, higherScoreActivity },
            12m,
            TimeSpan.FromHours(2));

        var selectedActivity = Assert.Single(selectedActivities);
        Assert.Equal(higherScoreActivity.Id, selectedActivity.Id);
    }

    [Fact]
    public void Vote_WhenParticipantChangesVote_ReplacesPreviousVote()
    {
        var participant = new Participant { Name = "Voter" };
        var activity = CreateActivity();
        var trip = new Trip();
        trip.Participants.Add(participant);
        trip.Activities.Add(activity);
        var service = new ActivityVotingService();

        service.Vote(trip, participant.Id, activity.Id, VoteType.Upvote);
        service.Vote(trip, participant.Id, activity.Id, VoteType.Downvote);

        Assert.Single(activity.Votes);
        Assert.Equal(0, service.GetUpvoteCount(activity));
        Assert.Equal(1, service.GetDownvoteCount(activity));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Vote_WhenParticipantOrActivityIsMissing_DoesNotAddVote(
        bool participantExists,
        bool activityExists)
    {
        var participant = new Participant { Name = "Voter" };
        var activity = CreateActivity();
        var trip = new Trip();
        if (participantExists)
        {
            trip.Participants.Add(participant);
        }
        if (activityExists)
        {
            trip.Activities.Add(activity);
        }
        var service = new ActivityVotingService();

        Assert.Throws<KeyNotFoundException>(() =>
            service.Vote(trip, participant.Id, activity.Id, VoteType.Upvote));

        Assert.Empty(activity.Votes);
    }

    [Fact]
    public void Vote_WhenVoteTypeIsUndefined_DoesNotChangeExistingVote()
    {
        var participant = new Participant { Name = "Voter" };
        var activity = CreateActivity();
        var trip = new Trip();
        trip.Participants.Add(participant);
        trip.Activities.Add(activity);
        activity.Votes[participant.Id] = VoteType.Upvote;
        var service = new ActivityVotingService();

        Assert.Throws<ArgumentException>(() =>
            service.Vote(trip, participant.Id, activity.Id, (VoteType)999));

        Assert.Equal(VoteType.Upvote, Assert.Single(activity.Votes).Value);
    }

    [Fact]
    public void RemoveVote_WhenVoteExists_RemovesOnlyRequestedParticipantsVote()
    {
        var participant = new Participant { Name = "Voter" };
        var activity = CreateActivity();
        var otherParticipantId = Guid.NewGuid();
        var trip = new Trip();
        trip.Participants.Add(participant);
        trip.Activities.Add(activity);
        activity.Votes[participant.Id] = VoteType.Upvote;
        activity.Votes[otherParticipantId] = VoteType.Downvote;
        var service = new ActivityVotingService();

        Assert.True(service.RemoveVote(trip, participant.Id, activity.Id));
        Assert.False(service.RemoveVote(trip, participant.Id, activity.Id));
        Assert.Equal(otherParticipantId, Assert.Single(activity.Votes).Key);
    }

    [Fact]
    public void PrioritizeByVotes_WhenNetScoresTie_PrefersMoreUpvotes()
    {
        var fewerVotes = CreateActivity();
        var moreVotes = CreateActivity();
        fewerVotes.Votes[Guid.NewGuid()] = VoteType.Upvote;
        moreVotes.Votes[Guid.NewGuid()] = VoteType.Upvote;
        moreVotes.Votes[Guid.NewGuid()] = VoteType.Upvote;
        moreVotes.Votes[Guid.NewGuid()] = VoteType.Downvote;
        var service = new ActivityVotingService();

        var prioritized = service.PrioritizeByVotes(new[] { fewerVotes, moreVotes });

        Assert.Equal(new[] { moreVotes.Id, fewerVotes.Id }, prioritized.Select(activity => activity.Id));
    }

    private static TripActivity CreateActivity()
    {
        return new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1),
            estimatedCost: 12m);
    }
}
