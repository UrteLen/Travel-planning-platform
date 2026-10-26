namespace TravelPlanner.Models;

public class StoredTripVersion
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TripId { get; init; }
    public int VersionNumber { get; init; }
    public DateTime Timestamp { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public int? RestoredFromVersion { get; init; }
    public string BudgetJson { get; init; } = "{}";
    public string PlanJson { get; init; } = "[]";
    public string ActivitiesJson { get; init; } = "[]";
}
