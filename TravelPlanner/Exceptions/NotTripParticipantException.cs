namespace TravelPlanner.Exceptions;

public class NotTripParticipantException : Exception
{
    public Guid TripId { get; }

    public NotTripParticipantException(Guid tripId)
        : base("The user is not a participant of this trip.")
    {
        TripId = tripId;
    }
}