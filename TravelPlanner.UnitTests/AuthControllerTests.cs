using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Controllers;
using TravelPlanner.Data;
using TravelPlanner.Exceptions;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class AuthControllerTests
{
    private sealed class FakeTokenService : ITokenService
    {
        public string CreateToken(User user) => $"token-for-{user.Email}";
    }

    private static AuthController CreateController(TravelPlannerDbContext? db)
    {
        UserService userService = db is null
            ? null!
            : new UserService(db, new PasswordHasher<User>(), new LoginAttemptTracker(TimeProvider.System));

        return new AuthController(userService, new FakeTokenService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Theory]
    [InlineData("", "Name", "Password123!")]
    [InlineData("  ", "Name", "Password123!")]
    [InlineData("a@b.c", "", "Password123!")]
    [InlineData("a@b.c", "Name", "short")]
    public async Task RegisterAsync_WithInvalidInput_ReturnsBadRequest(string email, string name, string password)
    {
        var controller = CreateController(null);

        var result = await controller.RegisterAsync(new RegisterRequest(email, name, password));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RegisterAsync_WithoutPassword_ReturnsBadRequest()
    {
        var controller = CreateController(null);

        var result = await controller.RegisterAsync(new RegisterRequest("a@b.c", "Name", null!));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RegisterAsync_WithValidInput_ReturnsTokenAndUser()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.RegisterAsync(new RegisterRequest("new@example.com", "New User", "Password123!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var auth = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("token-for-new@example.com", auth.Token);
        Assert.Equal("New User", auth.DisplayName);
        Assert.NotEqual(Guid.Empty, auth.UserId);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsTokenAndUser()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);
        await controller.RegisterAsync(new RegisterRequest("new@example.com", "New User", "Password123!"));

        var result = await controller.LoginAsync(new LoginRequest("new@example.com", "Password123!"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var auth = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("token-for-new@example.com", auth.Token);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_LetsTheExceptionBubbleUpToTheHandler()
    {
        using var db = TestDb.Create();
        var controller = CreateController(db);
        await controller.RegisterAsync(new RegisterRequest("new@example.com", "New User", "Password123!"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => controller.LoginAsync(new LoginRequest("new@example.com", "wrong-password")));
    }

    [Fact]
    public void Me_ReturnsTheClaimsFromTheToken()
    {
        var controller = CreateController(null);
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", "user-id-1"),
            new Claim("email", "user@example.com"),
            new Claim("name", "Test User")
        }, "test");
        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        var result = controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("user-id-1", json);
        Assert.Contains("user@example.com", json);
        Assert.Contains("Test User", json);
    }
}
