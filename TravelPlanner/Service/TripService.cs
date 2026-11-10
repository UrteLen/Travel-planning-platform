using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Enums;
using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class TripService
{
    private readonly TravelPlannerDbContext _db;

    public TripService(TravelPlannerDbContext db)
    {
        _db = db;
    }

    public async Task<(Trip? Trip, string? ValidationError)> CreateTripAsync(
        string name,
        string destination,
        DateTime startDate,
        DateTime endDate,
        string organizerName)
    {
        if (string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(destination)
            || string.IsNullOrWhiteSpace(organizerName))
        {
            return (null, "Name, destination and organizer name are required");
        }

        if (endDate < startDate)
        {
            return (null, "End date cannot be earlier than start date");
        }

        if (startDate.Date < DateTime.Today)
        {
            return (null, "Start date cannot be in the past");
        }

        var organizer = new Participant
        {
            Name = organizerName,
            Role = UserRole.Organizer
        };

        var trip = new Trip
        {
            Name = name,
            Destination = destination,
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            InviteCode = GenerateInviteCode(),
            Participants = new List<Participant> { organizer }
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();
        return (trip, null);
    }

    public async Task<Trip?> JoinTripAsync(
        string inviteCode,
        string participantName,
        UserRole role = UserRole.Participant)
    {
        var trip = await FindTripByCodeForUpdateAsync(inviteCode);

        if (trip is null)
        {
            return null;
        }

        try
        {
            var newParticipant = new Participant
            {
                Name = participantName,
                Role = role
            };

            trip.Participants.Add(newParticipant);
            await SaveChangesAsync();
            return trip;
        }
        finally
        {
            await RollbackTransactionAsync();
        }
    }

    public async Task<Trip?> FindTripByCodeAsync(string inviteCode)
    {
        return await TripsWithDetails()
            .FirstOrDefaultAsync(trip => trip.InviteCode == inviteCode);
    }

    public async Task<Trip?> FindTripByIdAsync(Guid tripId)
    {
        return await TripsWithDetails()
            .FirstOrDefaultAsync(trip => trip.Id == tripId);
    }

    // Use these instead of FindTripBy... in any endpoint that changes the trip.
    // They lock the trip's row until SaveChangesAsync, so two requests for the same trip
    // run one after the other and neither overwrites the other's change.
    public async Task<Trip?> FindTripByIdForUpdateAsync(Guid tripId)
    {
        await _db.Database.BeginTransactionAsync();

        Trip? trip;

        try
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM \"Trips\" WHERE \"Id\" = {tripId} FOR UPDATE");

            trip = await FindTripByIdAsync(tripId);
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }

        if (trip is null)
        {
            await RollbackTransactionAsync();
        }

        return trip;
    }

    public async Task<Trip?> FindTripByCodeForUpdateAsync(string inviteCode)
    {
        await _db.Database.BeginTransactionAsync();

        Trip? trip;

        try
        {
            await _db.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM \"Trips\" WHERE \"InviteCode\" = {inviteCode} FOR UPDATE");

            trip = await FindTripByCodeAsync(inviteCode);
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }

        if (trip is null)
        {
            await RollbackTransactionAsync();
        }

        return trip;
    }

    // Saves and releases the lock. Call RollbackTransactionAsync in finally
    // to release an unfinished transaction on validation errors or early returns.
    public async Task SaveChangesAsync()
    {
        var transaction = _db.Database.CurrentTransaction;

        try
        {
            await _db.SaveChangesAsync();

            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        var transaction = _db.Database.CurrentTransaction;

        if (transaction is not null)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private IQueryable<Trip> TripsWithDetails()
    {
        return _db.Trips
            .Include(trip => trip.Participants)
            .Include(trip => trip.Activities)
            .Include(trip => trip.ScheduledActivities)
                .ThenInclude(scheduledActivity => scheduledActivity.Activity)
            .Include(trip => trip.Budget)
                .ThenInclude(budget => budget.Expenses)
                    .ThenInclude(expense => expense.Participant)
            .Include(trip => trip.Plan)
                .ThenInclude(visit => visit.Place)
            .AsSplitQuery();
    }
}
