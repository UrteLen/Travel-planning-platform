using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Exceptions;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public class TripVersionService
{
    private readonly TravelPlannerDbContext _db;

    public TripVersionService(TravelPlannerDbContext db)
    {
        _db = db;
    }

    public Task<TripVersion> SaveVersionAsync(
        Guid tripId,
        Budget budget,
        List<PlannedVisit> plan,
        List<TripActivity> activities,
        string changedBy)
    {
        return AddVersionAsync(
            tripId,
            budget,
            plan,
            activities,
            changedBy,
            restoredFromVersion: null);
    }

    public async Task<List<TripVersionHistoryResponse>?> GetHistoryAsync(Guid tripId)
    {
        if (!await _db.Trips.AnyAsync(trip => trip.Id == tripId))
        {
            return null;
        }

        return await _db.TripVersions
            .Where(version => version.TripId == tripId)
            .OrderBy(version => version.VersionNumber)
            .Select(version => new TripVersionHistoryResponse(
                version.VersionNumber,
                version.Timestamp,
                version.ChangedBy,
                version.RestoredFromVersion))
            .ToListAsync();
    }

    public async Task<TripVersion?> GetVersionAsync(Guid tripId, int versionNumber)
    {
        var stored = await _db.TripVersions
            .FirstOrDefaultAsync(version =>
                version.TripId == tripId
                && version.VersionNumber == versionNumber);

        return stored is null ? null : ToTripVersion(stored);
    }

    public async Task<TripVersion> RestoreAsync(Guid tripId, int versionNumber, string changedBy)
    {
        var restored = await GetVersionAsync(tripId, versionNumber)
            ?? throw new VersionNotFoundException(tripId, versionNumber);

        return await AddVersionAsync(
            tripId,
            restored.BudgetSnapshot,
            restored.PlanSnapshot,
            restored.ActivitiesSnapshot,
            changedBy,
            versionNumber);
    }

    private async Task<TripVersion> AddVersionAsync(
        Guid tripId,
        Budget budget,
        List<PlannedVisit> plan,
        List<TripActivity> activities,
        string changedBy,
        int? restoredFromVersion)
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
