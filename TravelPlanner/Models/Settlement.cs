namespace TravelPlanner.Models
{
    public record Settlement(Guid FromParticipantId, Guid ToParticipantId, decimal Amount);
}