using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelPlanner.Enums;
using TravelPlanner.Requests;
using TravelPlanner.Responses;

namespace TravelPlanner.IntegrationTests;

public class ChecklistIntegrationTests
{
    private const string BaseAddress = "https://localhost:7257";

    [Fact]
    public async Task AddItem_WhenIdenticalRequestsRunAtOnce_StoresOnlyOneItem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await CreateLoggedInClientAsync(cancellationToken);
        var tripId = await CreateTripAsync(client, cancellationToken);

        var requests = Enumerable.Range(0, 8).Select(_ => client.PostAsJsonAsync(
            $"/api/trips/{tripId}/checklist",
            new AddChecklistItemRequest("Passport", ChecklistCategory.Documents),
            cancellationToken));
        var responses = await Task.WhenAll(requests);

        try
        {
            Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            Assert.Equal(7, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        var checklist = await client.GetFromJsonAsync<JsonElement>(
            $"/api/trips/{tripId}/checklist",
            cancellationToken);
        Assert.Equal(1, checklist.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task AddItem_WhenSameTextWithDifferentCase_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await CreateLoggedInClientAsync(cancellationToken);
        var tripId = await CreateTripAsync(client, cancellationToken);
        var url = $"/api/trips/{tripId}/checklist";

        using var first = await client.PostAsJsonAsync(
            url,
            new AddChecklistItemRequest("Passport", ChecklistCategory.Documents),
            cancellationToken);
        using var second = await client.PostAsJsonAsync(
            url,
            new AddChecklistItemRequest("passport", ChecklistCategory.Documents),
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Get_WhenUserIsNotTripParticipant_ReturnsForbidden()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var owner = await CreateLoggedInClientAsync(cancellationToken);
        using var stranger = await CreateLoggedInClientAsync(cancellationToken);
        var tripId = await CreateTripAsync(owner, cancellationToken);

        using var response = await stranger.GetAsync($"/api/trips/{tripId}/checklist", cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpClient> CreateLoggedInClientAsync(CancellationToken cancellationToken)
    {
        var client = new HttpClient { BaseAddress = new Uri(BaseAddress) };

        using var registerResponse = await client.PostAsJsonAsync(
            "/api/Auth/register",
            new RegisterRequest(
                $"integration-{Guid.NewGuid():N}@example.com",
                "Checklist Test User",
                "IntegrationTest123!"),
            cancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);

        return client;
    }

    private static async Task<Guid> CreateTripAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var startDate = DateTime.Today.AddDays(7);

        using var response = await client.PostAsJsonAsync(
            "/api/Trip/create",
            new CreateTripRequest("Checklist trip", "Rome", startDate, startDate.AddDays(2), "Checklist Test User"),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return created.GetProperty("id").GetGuid();
    }
}