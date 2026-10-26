using System.Text.Json;
using TravelPlanner.Data;
using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class TripVersionService
{
    private readonly TravelPlannerDbContext _db;

    public TripVersionService(TravelPlannerDbContext db)
    {
        _db = db;
    }

    public TripVersion SaveVersion(Guid tripId, Budget budget, List<PlannedVisit> plan, List<TripActivity> activities, string changedBy)
    {
        return AddVersion(tripId, budget, plan, activities, changedBy, restoredFromVersion: null);
    }

    public List<TripVersion> GetHistory(Guid tripId)
    {
        return _db.TripVersions
            .Where(v => v.TripId == tripId)
            .OrderBy(v => v.VersionNumber)
            .AsEnumerable()
            .Select(ToTripVersion)
            .ToList();
    }

    public TripVersion? GetVersion(Guid tripId, int versionNumber)
    {
        var stored = _db.TripVersions
            .FirstOrDefault(v => v.TripId == tripId && v.VersionNumber == versionNumber);

        return stored is null ? null : ToTripVersion(stored);
    }

    public TripVersion Restore(Guid tripId, int versionNumber, string changedBy)
    {
        var restored = GetVersion(tripId, versionNumber)
            ?? throw new ArgumentException("Version not found");

        return AddVersion(tripId, restored.BudgetSnapshot, restored.PlanSnapshot,
            restored.ActivitiesSnapshot, changedBy, versionNumber);
    }

    private TripVersion AddVersion(Guid tripId, Budget budget, List<PlannedVisit> plan,
        List<TripActivity> activities, string changedBy, int? restoredFromVersion)
    {
        var stored = new StoredTripVersion
        {
            TripId = tripId,
            VersionNumber = _db.TripVersions.Count(v => v.TripId == tripId) + 1,
            Timestamp = DateTime.UtcNow,
            ChangedBy = changedBy,
            RestoredFromVersion = restoredFromVersion,
            BudgetJson = JsonSerializer.Serialize(budget),
            PlanJson = JsonSerializer.Serialize(plan),
            ActivitiesJson = JsonSerializer.Serialize(activities)
        };

        _db.TripVersions.Add(stored);
        return ToTripVersion(stored);
    }

    private static TripVersion ToTripVersion(StoredTripVersion stored)
    {
        return new TripVersion(
            stored.VersionNumber,
            stored.Timestamp,
            stored.ChangedBy,
            JsonSerializer.Deserialize<Budget>(stored.BudgetJson)!,
            JsonSerializer.Deserialize<List<PlannedVisit>>(stored.PlanJson)!,
            JsonSerializer.Deserialize<List<TripActivity>>(stored.ActivitiesJson)!,
            stored.RestoredFromVersion);
    }
}
