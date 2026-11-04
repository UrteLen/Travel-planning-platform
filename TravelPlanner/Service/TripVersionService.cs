using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TravelPlanner.Data;
using TravelPlanner.Models;
using TravelPlanner.Exceptions;

namespace TravelPlanner.Service;

public class TripVersionService
{
    private readonly TravelPlannerDbContext _db;

    public TripVersionService(TravelPlannerDbContext db)
    {
        _db = db;
    }

    public Task<TripVersion> SaveVersionAsync(Guid tripId, Budget budget, List<PlannedVisit> plan, List<TripActivity> activities, string changedBy)
    {
        return AddVersionAsync(tripId, budget, plan, activities, changedBy, restoredFromVersion: null);
    }

    public async Task<List<TripVersion>> GetHistoryAsync(Guid tripId)
    {
        var stored = await _db.TripVersions
            .Where(v => v.TripId == tripId)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync();

        return stored.Select(ToTripVersion).ToList();
    }

    public async Task<TripVersion?> GetVersionAsync(Guid tripId, int versionNumber)
    {
        var stored = await _db.TripVersions
            .FirstOrDefaultAsync(v => v.TripId == tripId && v.VersionNumber == versionNumber);

        return stored is null ? null : ToTripVersion(stored);
    }

    public async Task<TripVersion> RestoreAsync(Guid tripId, int versionNumber, string changedBy)
    {
        var restored = await GetVersionAsync(tripId, versionNumber)
            ?? throw new VersionNotFoundException(tripId, versionNumber);

        return await AddVersionAsync(tripId, restored.BudgetSnapshot, restored.PlanSnapshot,
            restored.ActivitiesSnapshot, changedBy, versionNumber);
    }

    private async Task<TripVersion> AddVersionAsync(Guid tripId, Budget budget, List<PlannedVisit> plan,
        List<TripActivity> activities, string changedBy, int? restoredFromVersion)
    {
        int versionNumber = await _db.TripVersions.CountAsync(v => v.TripId == tripId) + 1;

        var stored = new StoredTripVersion
        {
            TripId = tripId,
            VersionNumber = versionNumber,
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
