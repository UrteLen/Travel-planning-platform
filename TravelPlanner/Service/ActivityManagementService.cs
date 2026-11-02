using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Enums;
using TravelPlanner.Models;

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
        Category category,
        GeoLocation location,
        TimeSpan duration,
        decimal estimatedCost = 0m,
        bool optional = true,
        TimeSpan? openingTime = null,
        TimeSpan? closingTime = null)
    {
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
            category,
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
        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var trip = await FindTripForUpdateAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        var activity = trip.Activities
            .FirstOrDefault(candidate => candidate.Id == activityId)
            ?? throw new KeyNotFoundException("Activity not found in the trip.");

        var scheduledActivity = _activitySchedulingService.ScheduleActivity(
            trip, activity, startTime, speedKmh);

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
            trip, scheduledActivityId, startTime, speedKmh);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return scheduledActivity;
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
