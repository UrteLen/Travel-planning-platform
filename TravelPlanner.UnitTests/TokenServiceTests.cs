using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class TokenServiceTests
{
    private static TokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('k', 48)
            })
            .Build();
        return new TokenService(config);
    }

    private static User CreateUser() => new()
    {
        Email = "user@example.com",
        DisplayName = "Test User",
        PasswordHash = "secret-hash"
    };

    private static JwtSecurityToken Read(string jwt) => new JwtSecurityTokenHandler().ReadJwtToken(jwt);

    [Fact]
    public void CreateToken_ContainsSubEmailAndNameClaims()
    {
        var user = CreateUser();

        var token = Read(CreateService().CreateToken(user));

        Assert.Equal(user.Id.ToString(), token.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal(user.Email, token.Claims.First(c => c.Type == "email").Value);
        Assert.Equal(user.DisplayName, token.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public void CreateToken_UsesTravelPlannerIssuerAndAudience()
    {
        var token = Read(CreateService().CreateToken(CreateUser()));

        Assert.Equal("TravelPlanner", token.Issuer);
        Assert.Contains("TravelPlanner", token.Audiences);
    }

    [Fact]
    public void CreateToken_ExpiresInAboutEightHours()
    {
        var token = Read(CreateService().CreateToken(CreateUser()));

        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddHours(7.9), DateTime.UtcNow.AddHours(8.1));
    }

    [Fact]
    public void CreateToken_DoesNotLeakThePasswordHash()
    {
        var user = CreateUser();

        var jwt = CreateService().CreateToken(user);
        var token = Read(jwt);

        Assert.DoesNotContain(token.Claims, c => c.Value == user.PasswordHash);
    }

    [Fact]
    public void CreateToken_IsSignedWithHmacSha256()
    {
        var token = Read(CreateService().CreateToken(CreateUser()));

        Assert.Equal("HS256", token.Header.Alg);
    }
}
