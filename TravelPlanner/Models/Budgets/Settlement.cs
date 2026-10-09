namespace TravelPlanner.Models.Budgets
{
    public record Settlement(Guid FromParticipantId, Guid ToParticipantId, decimal Amount);
}