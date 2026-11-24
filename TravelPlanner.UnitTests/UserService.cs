using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Exceptions;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class UserServiceTests
{
    private const string Password = "Password123!";
    private const string Address = "127.0.0.1";

    private static UserService CreateService(TravelPlannerDbContext db) =>
        new(db, new PasswordHasher<User>(), new LoginAttemptTracker(TimeProvider.System));

    [Fact]
    public async Task RegisterAsync_NormalizesEmailTrimsNameAndHashesPassword()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);

        var user = await service.RegisterAsync("  User@Example.COM ", "  Test User  ", Password);

        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.False(string.IsNullOrEmpty(user.PasswordHash));
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, Password));
        Assert.Equal(1, await db.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExistsInDifferentCase_Throws()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);
        await service.RegisterAsync("user@example.com", "First", Password);

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => service.RegisterAsync("USER@example.com", "Second", Password));

        Assert.Equal(1, await db.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTheUser()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);
        var registered = await service.RegisterAsync("user@example.com", "Test User", Password);

        var user = await service.LoginAsync("USER@example.com ", Password, Address);

        Assert.Equal(registered.Id, user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsInvalidCredentials()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync("nobody@example.com", Password, Address));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentials()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);
        await service.RegisterAsync("user@example.com", "Test User", Password);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync("user@example.com", "wrong-password", Address));
    }

    [Fact]
    public async Task LoginAsync_AfterTooManyFailures_ThrowsTooManyAttemptsEvenForTheRightPassword()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);
        await service.RegisterAsync("user@example.com", "Test User", Password);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("user@example.com", "wrong-password", Address));
        }

        await Assert.ThrowsAsync<TooManyLoginAttemptsException>(
            () => service.LoginAsync("user@example.com", Password, Address));
    }

    [Fact]
    public async Task LoginAsync_AfterSuccessfulLogin_StartsCountingAttemptsFromZero()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);
        await service.RegisterAsync("user@example.com", "Test User", Password);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts - 1; i++)
        {
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("user@example.com", "wrong-password", Address));
        }

        await service.LoginAsync("user@example.com", Password, Address);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("user@example.com", "wrong-password", Address));
        }
    }

    [Fact]
    public async Task LoginAsync_CountsAttemptsPerEmailAndAddress()
    {
        using var db = TestDb.Create();
        var service = CreateService(db);
        await service.RegisterAsync("user@example.com", "Test User", Password);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            await Assert.ThrowsAsync<InvalidCredentialsException>(
                () => service.LoginAsync("user@example.com", "wrong-password", Address));
        }

        var user = await service.LoginAsync("user@example.com", Password, "10.0.0.2");

        Assert.Equal("user@example.com", user.Email);
    }
}
