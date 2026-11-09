using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Data;
using TravelPlanner.Exceptions;
using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class UserService
{
    private readonly TravelPlannerDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(TravelPlannerDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(string email, string displayName, string password)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            throw new EmailAlreadyRegisteredException(normalizedEmail);
        }

        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = displayName.Trim()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        var normalizedEmail = NormalizeEmail(email);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail)
            ?? throw new InvalidCredentialsException();

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        return user;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
