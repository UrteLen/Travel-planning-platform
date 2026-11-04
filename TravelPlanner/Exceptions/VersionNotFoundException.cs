namespace TravelPlanner.Exceptions;

public class VersionNotFoundException : Exception
{
    public Guid TripId { get; }

    public int VersionNumber { get; }

    public VersionNotFoundException(Guid tripId, int versionNumber)
        : base($"Version {versionNumber} of trip {tripId} was not found.")
    {
        TripId = tripId;
        VersionNumber = versionNumber;
    }
}