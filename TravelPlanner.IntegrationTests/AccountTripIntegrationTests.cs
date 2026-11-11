using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelPlanner.Requests;
using TravelPlanner.Responses;

namespace TravelPlanner.IntegrationTests;

public class AccountTripIntegrationTests
{
    [Fact]
    public async Task Mine_WhenLoggedInUserCreatesTrip_ReturnsThatTripAsOrganizer()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://localhost:7257");
        var cancellationToken = TestContext.Current.CancellationToken;

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new RegisterRequest(
                $"integration-{Guid.NewGuid():N}@example.com",
                "Account Test User",
                "IntegrationTest123!"),
            cancellationToken);
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);

        var startDate = DateTime.Today.AddDays(7);
        using var createResponse = await client.PostAsJsonAsync(
            "/api/Trip/create",
            new CreateTripRequest("Weekend in Rome", "Rome", startDate, startDate.AddDays(2), "Account Test User"),
            cancellationToken);
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var createdTripId = created.GetProperty("id").GetGuid();
        var participantIds = created.GetProperty("participants")
            .EnumerateObject()
            .Select(participant => Guid.Parse(participant.Name))
            .ToList();

        var myTrips = await client.GetFromJsonAsync<JsonElement[]>("/api/Trip/mine", cancellationToken);

        Assert.NotNull(myTrips);
        var myTrip = Assert.Single(myTrips);
        Assert.Equal(createdTripId, myTrip.GetProperty("id").GetGuid());
        Assert.Equal("Organizer", myTrip.GetProperty("role").GetString());
        Assert.Contains(myTrip.GetProperty("participantId").GetGuid(), participantIds);
    }

    [Fact]
    public async Task Mine_WithoutToken_ReturnsUnauthorized()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://localhost:7257");

        using var response = await client.GetAsync(
            "/api/Trip/mine",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}