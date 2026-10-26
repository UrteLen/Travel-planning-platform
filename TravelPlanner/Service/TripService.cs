using TravelPlanner.Enums;
using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class TripService
{
    private readonly List<Trip> _trips = new();

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
            StartDate = startDate,
            EndDate = endDate,
            InviteCode = GenerateInviteCode(),
            Participants = new List<Participant> { organizer}
        };

        _trips.Add(trip);
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
        return trip;
    }

    public Trip? FindTripByCode(string inviteCode)
    {
        return _trips.FirstOrDefault(t => t.InviteCode == inviteCode);
    }

    public Trip? FindTripById(Guid tripId)
    {
        return _trips.FirstOrDefault(t => t.Id == tripId);
    }

    public string GenerateInviteCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }
}