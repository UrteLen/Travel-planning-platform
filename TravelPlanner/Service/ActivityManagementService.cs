using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public class ActivityManagementService
{
    private readonly TravelPlannerDbContext _db;
    private readonly ActivityService _activityService;
    private readonly ActivityImportService _activityImportService;
    private readonly ActivityVotingService _activityVotingService;
    private readonly ActivitySchedulingService _activitySchedulingService;

    public ActivityManagementService(
        TravelPlannerDbContext db,
        ActivityService activityService,
        ActivityImportService activityImportService,
        ActivityVotingService activityVotingService,
        ActivitySchedulingService activitySchedulingService)
    {
        _db = db;
        _activityService = activityService;
        _activityImportService = activityImportService;
        _activityVotingService = activityVotingService;
        _activitySchedulingService = activitySchedulingService;
    }

    public async Task<TripActivity?> AddActivityAsync(
        Guid tripId,
        string name,
        string category,
        double latitude,
        double longitude,
        double durationHours,
        decimal estimatedCost = 0m,
        bool optional = true,
        TimeSpan? openingTime = null,
        TimeSpan? closingTime = null)
    {
        if (!Enum.TryParse<Category>(category, true, out var parsedCategory)
            || !Enum.IsDefined(parsedCategory))
        {
            throw new ArgumentException("Invalid category.");
        }

        if (!double.IsFinite(durationHours) || durationHours <= 0)
        {
            throw new ArgumentException(
                "Activity duration must be a finite number greater than zero.");
        }

        var location = new GeoLocation(latitude, longitude);
        var duration = TimeSpan.FromHours(durationHours);

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId);

        if (trip is null)
        {
            return null;
        }

        var activity = _activityService.AddActivity(
            trip,
            name,
            parsedCategory,
            location,
            duration,
            estimatedCost,
            optional,
            openingTime,
            closingTime);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return activity;
    }

    public async Task<List<TripActivity>?> GetPrioritizedActivitiesAsync(Guid tripId)
    {
        var activities = await FindTripActivitiesAsync(tripId);

        if (activities is null)
        {
            return null;
        }

        return _activityVotingService.PrioritizeByVotes(activities);
    }

    public async Task<(List<TripActivity>? Activities, string? ValidationError)> GetSelectedActivitiesAsync(
        Guid tripId,
        decimal? budget,
        double? hours)
    {
        var activities = await FindTripActivitiesAsync(tripId);

        if (activities is null)
        {
            return (null, null);
        }

        if (budget < 0)
        {
            return (null, "Budget must be non-negative.");
        }

        if (!hours.HasValue || !double.IsFinite(hours.Value) || hours.Value < 0)
        {
            return (null, "Hours are required and must be non-negative and finite.");
        }

        TimeSpan availableTime;
        try
        {
            availableTime = TimeSpan.FromHours(hours.Value);
        }
        catch (OverflowException)
        {
            return (null, "Hours value is too large.");
        }

        var selectedActivities = _activityVotingService.SelectActivitiesForPlan(
            activities,
            budget,
            availableTime);

        return (selectedActivities, null);
    }

    public async Task<(VoteResultsResponse? Results, string? NotFoundError)> GetVoteResultsAsync(
        Guid tripId,
        Guid activityId,
        Guid? participantId)
    {
        if (!await _db.Trips.AnyAsync(trip => trip.Id == tripId))
        {
            return (null, "Trip not found.");
        }

        var activity = await _db.Trips
            .Where(trip => trip.Id == tripId)
            .SelectMany(trip => trip.Activities)
            .SingleOrDefaultAsync(activity => activity.Id == activityId);

        if (activity is null)
        {
            return (null, "Activity not found in the trip.");
        }

        VoteType? participantVote = null;

        if (participantId.HasValue
            && activity.Votes.TryGetValue(participantId.Value, out var existingVote))
        {
            participantVote = existingVote;
        }

        var results = new VoteResultsResponse(
            _activityVotingService.GetUpvoteCount(activity),
            _activityVotingService.GetDownvoteCount(activity),
            participantVote);

        return (results, null);
    }

    public async Task<List<ScheduledActivity>?> GetScheduleAsync(
        Guid tripId,
        DateTime? date)
    {
        if (!await _db.Trips.AnyAsync(trip => trip.Id == tripId))
        {
            return null;
        }

        var scheduledActivities = await _db.Trips
            .Where(trip => trip.Id == tripId)
            .SelectMany(trip => trip.ScheduledActivities)
            .Include(scheduledActivity => scheduledActivity.Activity)
            .ToListAsync();

        if (date.HasValue)
        {
            return _activitySchedulingService.GetDailySchedule(
                scheduledActivities,
                date.Value);
        }

        return scheduledActivities;
    }

    public async Task<List<TripActivity>?> ImportActivitiesAsync(
        Guid tripId,
        Stream stream)
    {
        var activities =
            await _activityImportService.LoadActivitiesFromJsonAsync(stream);

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId);

        if (trip is null)
        {
            return null;
        }

        foreach (var activity in activities)
        {
            trip.Activities.Add(activity);
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return activities;
    }

    public async Task VoteAsync(
        Guid tripId,
        Guid participantId,
        Guid activityId,
        VoteType voteType)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        _activityVotingService.Vote(trip, participantId, activityId, voteType);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RemoveVoteAsync(
        Guid tripId,
        Guid participantId,
        Guid activityId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        if (!_activityVotingService.RemoveVote(trip, participantId, activityId))
        {
            throw new KeyNotFoundException("Vote not found.");
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<ScheduledActivity> ScheduleActivityAsync(
        Guid tripId,
        Guid activityId,
        DateTime startTime,
        double speedKmh)
    {
        if (activityId == Guid.Empty
            || startTime == default
            || !double.IsFinite(speedKmh)
            || speedKmh <= 0)
        {
            throw new ArgumentException(
                "Valid ActivityId, StartTime and positive SpeedKmh are required.");
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        var activity = trip.Activities
            .FirstOrDefault(candidate => candidate.Id == activityId)
            ?? throw new KeyNotFoundException("Activity not found in the trip.");

        var scheduledActivity = _activitySchedulingService.ScheduleActivity(
            trip,
            activity,
            DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified),
            speedKmh);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return scheduledActivity;
    }

    public async Task RemoveScheduledActivityAsync(
        Guid tripId,
        Guid scheduledActivityId)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        if (!_activitySchedulingService.RemoveScheduledActivity(trip, scheduledActivityId))
        {
            throw new KeyNotFoundException("Scheduled activity not found.");
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<ScheduledActivity> UpdateScheduledActivityAsync(
        Guid tripId,
        Guid scheduledActivityId,
        DateTime startTime,
        double speedKmh)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        var scheduledActivity = _activitySchedulingService.UpdateScheduledActivity(
            trip,
            scheduledActivityId,
            DateTime.SpecifyKind(startTime, DateTimeKind.Unspecified),
            speedKmh);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return scheduledActivity;
    }

    private Task<List<TripActivity>?> FindTripActivitiesAsync(Guid tripId)
    {
        return _db.Trips
            .Where(trip => trip.Id == tripId)
            .Select(trip => trip.Activities)
            .SingleOrDefaultAsync();
    }

    private async Task<Trip?> FindTripForUpdateAsync(Guid tripId)
    {
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM \"Trips\" WHERE \"Id\" = {tripId} FOR UPDATE");

        return await _db.Trips
            .Include(trip => trip.Participants)
            .Include(trip => trip.Activities)
            .Include(trip => trip.ScheduledActivities)
                .ThenInclude(scheduledActivity => scheduledActivity.Activity)
            .Include(trip => trip.Budget)
            .AsSplitQuery()
            .FirstOrDefaultAsync(trip => trip.Id == tripId);
    }
}
