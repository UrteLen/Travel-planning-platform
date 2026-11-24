using TravelPlanner.Enums;
using TravelPlanner.Exceptions;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class TripVersionServiceTests
{
    private static async Task<Guid> CreateTripAsync(TravelPlanner.Data.TravelPlannerDbContext db)
    {
        var start = DateTime.Today.AddDays(5);
        var (trip, _) = await new TripService(db).CreateTripAsync("Rome", "Rome", start, start.AddDays(2), "Urte");
        return trip!.Id;
    }

    private static async Task SaveVersionAsync(
        TravelPlanner.Data.TravelPlannerDbContext db,
        TripVersionService service,
        Guid tripId,
        Budget budget,
        string changedBy)
    {
        await service.SaveVersionAsync(tripId, budget, new List<PlannedVisit>(), new List<TripActivity>(), changedBy);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetHistoryAsync_ForUnknownTrip_ReturnsNull()
    {
        using var db = TestDb.Create();
        var service = new TripVersionService(db);

        Assert.Null(await service.GetHistoryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetHistoryAsync_ForTripWithoutVersions_ReturnsEmptyList()
    {
        using var db = TestDb.Create();
        var tripId = await CreateTripAsync(db);

        var history = await new TripVersionService(db).GetHistoryAsync(tripId);

        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public async Task SaveVersionAsync_NumbersVersionsInOrder()
    {
        using var db = TestDb.Create();
        var tripId = await CreateTripAsync(db);
        var service = new TripVersionService(db);

        await SaveVersionAsync(db, service, tripId, new Budget(), "alice");
        await SaveVersionAsync(db, service, tripId, new Budget(), "bob");

        var history = await service.GetHistoryAsync(tripId);

        Assert.NotNull(history);
        Assert.Equal(new[] { 1, 2 }, history.Select(h => h.VersionNumber));
        Assert.Equal(new[] { "alice", "bob" }, history.Select(h => h.ChangedBy));
    }

    [Fact]
    public async Task GetVersionAsync_ReturnsTheSavedSnapshot()
    {
        using var db = TestDb.Create();
        var tripId = await CreateTripAsync(db);
        var service = new TripVersionService(db);
        var budget = new Budget();
        budget.CategoryLimits[Category.Food] = 100m;
        await SaveVersionAsync(db, service, tripId, budget, "alice");

        var version = await service.GetVersionAsync(tripId, 1);

        Assert.NotNull(version);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal("alice", version.ChangedBy);
        Assert.Equal(100m, version.BudgetSnapshot.CategoryLimits[Category.Food]);
        Assert.Null(version.RestoredFromVersion);
    }

    [Fact]
    public async Task GetVersionAsync_ForMissingVersion_ReturnsNull()
    {
        using var db = TestDb.Create();
        var tripId = await CreateTripAsync(db);

        Assert.Null(await new TripVersionService(db).GetVersionAsync(tripId, 7));
    }

    [Fact]
    public async Task RestoreAsync_CreatesANewVersionWithTheOldContent()
    {
        using var db = TestDb.Create();
        var tripId = await CreateTripAsync(db);
        var service = new TripVersionService(db);
        var budget = new Budget();
        budget.CategoryLimits[Category.Food] = 100m;
        await SaveVersionAsync(db, service, tripId, budget, "alice");

        var restored = await service.RestoreAsync(tripId, 1, "bob");
        await db.SaveChangesAsync();

        Assert.Equal(2, restored.VersionNumber);
        Assert.Equal(1, restored.RestoredFromVersion);
        Assert.Equal("bob", restored.ChangedBy);
        Assert.Equal(100m, restored.BudgetSnapshot.CategoryLimits[Category.Food]);
    }

    [Fact]
    public async Task RestoreAsync_ForMissingVersion_ThrowsVersionNotFound()
    {
        using var db = TestDb.Create();
        var tripId = await CreateTripAsync(db);
        var service = new TripVersionService(db);

        var exception = await Assert.ThrowsAsync<VersionNotFoundException>(
            () => service.RestoreAsync(tripId, 9, "bob"));

        Assert.Equal(tripId, exception.TripId);
        Assert.Equal(9, exception.VersionNumber);
    }
}
