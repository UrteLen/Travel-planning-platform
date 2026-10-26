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
        var trip = FindTripByCode(inviteCode);
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
        _db.SaveChanges();
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

    public void SaveChanges()
    {
        _db.SaveChanges();
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