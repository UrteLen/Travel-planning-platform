using Microsoft.EntityFrameworkCore;
using TravelPlanner.Enums;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class TripServiceTests
{
    private static readonly DateTime NextWeek = DateTime.Today.AddDays(7);

    [Theory]
    [InlineData("", "Rome", "Organizer")]
    [InlineData("Trip", " ", "Organizer")]
    [InlineData("Trip", "Rome", "")]
    public async Task CreateTripAsync_WithBlankFields_ReturnsValidationError(string name, string destination, string organizer)
    {
        var service = new TripService(null!);

        var (trip, error) = await service.CreateTripAsync(name, destination, NextWeek, NextWeek.AddDays(1), organizer);

        Assert.Null(trip);
        Assert.Equal("Name, destination and organizer name are required", error);
    }

    [Fact]
    public async Task CreateTripAsync_WhenEndIsBeforeStart_ReturnsValidationError()
    {
        var service = new TripService(null!);

        var (trip, error) = await service.CreateTripAsync("Trip", "Rome", NextWeek, NextWeek.AddDays(-1), "Organizer");

        Assert.Null(trip);
        Assert.Equal("End date cannot be earlier than start date", error);
    }

    [Fact]
    public async Task CreateTripAsync_WhenStartIsInThePast_ReturnsValidationError()
    {
        var service = new TripService(null!);
        var yesterday = DateTime.Today.AddDays(-1);

        var (trip, error) = await service.CreateTripAsync("Trip", "Rome", yesterday, yesterday.AddDays(2), "Organizer");

        Assert.Null(trip);
        Assert.Equal("Start date cannot be in the past", error);
    }

    [Fact]
    public async Task CreateTripAsync_WithValidInput_SavesTheTripWithTheOrganizer()
    {
        using var db = TestDb.Create();
        var service = new TripService(db);
        var userId = Guid.NewGuid();

        var (trip, error) = await service.CreateTripAsync(
            "Weekend in Rome", "Rome", NextWeek, NextWeek.AddDays(2), "Urte", userId);

        Assert.Null(error);
        Assert.NotNull(trip);
        var organizer = Assert.Single(trip.Participants);
        Assert.Equal("Urte", organizer.Name);
        Assert.Equal(UserRole.Organizer, organizer.Role);
        Assert.Equal(userId, organizer.UserId);
        Assert.Equal(6, trip.InviteCode.Length);
        Assert.Equal(DateTimeKind.Utc, trip.StartDate.Kind);
        Assert.Equal(1, await db.Trips.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FindTripsByUserAsync_ReturnsOnlyThatUsersTripsNewestStartFirst()
    {
        using var db = TestDb.Create();
        var service = new TripService(db);
        var user = Guid.NewGuid();
        var otherUser = Guid.NewGuid();

        await service.CreateTripAsync("Later", "Rome", NextWeek.AddDays(10), NextWeek.AddDays(12), "A", user);
        await service.CreateTripAsync("Sooner", "Paris", NextWeek, NextWeek.AddDays(2), "A", user);
        await service.CreateTripAsync("Not mine", "Oslo", NextWeek, NextWeek.AddDays(2), "B", otherUser);

        var trips = await service.FindTripsByUserAsync(user);

        Assert.Equal(new[] { "Later", "Sooner" }, trips.Select(t => t.Name));
    }

    [Fact]
    public async Task FindTripsByUserAsync_ForUserWithoutTrips_ReturnsEmptyList()
    {
        using var db = TestDb.Create();
        var service = new TripService(db);

        Assert.Empty(await service.FindTripsByUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public void GenerateInviteCode_IsSixUppercaseHexCharacters()
    {
        var code = new TripService(null!).GenerateInviteCode();

        Assert.Equal(6, code.Length);
        Assert.Equal(code.ToUpperInvariant(), code);
        Assert.All(code, c => Assert.True(Uri.IsHexDigit(c)));
    }
}
