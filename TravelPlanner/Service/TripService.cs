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

    public Trip CreateTrip(string name, string destination, DateTime startDate, DateTime endDate, string organizerName)
    {
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
        _db.SaveChanges();
        return trip;
    }

    public Trip? JoinTrip(string inviteCode, string participantName, UserRole role = UserRole.Participant)
    {
        var trip = FindTripByCodeForUpdate(inviteCode);
        if (trip is null)
        {
            return null;
        }

        var newParticipant = new Participant
        {
            Name = participantName,
            Role = role
        };

        trip.Participants.Add(newParticipant);
        SaveChanges();
        return trip;
    }

    public Trip? FindTripByCode(string inviteCode)
    {
        return TripsWithDetails().FirstOrDefault(t => t.InviteCode == inviteCode);
    }

    public Trip? FindTripById(Guid tripId)
    {
        return TripsWithDetails().FirstOrDefault(t => t.Id == tripId);
    }

    // Use these instead of FindTripBy... in any endpoint that changes the trip.
    // They lock the trip's row until SaveChanges, so two requests for the same trip
    // run one after the other and neither overwrites the other's change.
    public Trip? FindTripByIdForUpdate(Guid tripId)
    {
        _db.Database.BeginTransaction();
        _db.Database.ExecuteSql($"SELECT 1 FROM \"Trips\" WHERE \"Id\" = {tripId} FOR UPDATE");
        return FindTripById(tripId);
    }

    public Trip? FindTripByCodeForUpdate(string inviteCode)
    {
        _db.Database.BeginTransaction();
        _db.Database.ExecuteSql($"SELECT 1 FROM \"Trips\" WHERE \"InviteCode\" = {inviteCode} FOR UPDATE");
        return FindTripByCode(inviteCode);
    }

    // Saves and releases the lock. If a request returns early without calling this,
    // the lock is released when the request ends and nothing is saved.
    public void SaveChanges()
    {
        _db.SaveChanges();
        _db.Database.CurrentTransaction?.Commit();
    }

    public string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private IQueryable<Trip> TripsWithDetails()
    {
        return _db.Trips
            .Include(t => t.Participants)
            .Include(t => t.Activities)
            .Include(t => t.ScheduledActivities)
                .ThenInclude(s => s.Activity)
            .Include(t => t.Budget)
                .ThenInclude(b => b.Expenses)
                    .ThenInclude(e => e.Participant)
            .Include(t => t.Plan)
                .ThenInclude(v => v.Place)
            .AsSplitQuery();
    }
}