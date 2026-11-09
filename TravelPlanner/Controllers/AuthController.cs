using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const int MinPasswordLength = 8;

    private readonly UserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(UserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.DisplayName)
            || request.Password is null
            || request.Password.Length < MinPasswordLength)
        {
            return BadRequest(new { error = "Email, name and a password of at least 8 characters are required" });
        }

        var user = await _userService.RegisterAsync(request.Email, request.DisplayName, request.Password);

        return Ok(new AuthResponse(_tokenService.CreateToken(user), user.Id, user.DisplayName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(LoginRequest request)
    {
        var user = await _userService.LoginAsync(request.Email, request.Password);

        return Ok(new AuthResponse(_tokenService.CreateToken(user), user.Id, user.DisplayName));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirst("sub")?.Value,
            email = User.FindFirst("email")?.Value,
            name = User.FindFirst("name")?.Value
        });
    }
}
