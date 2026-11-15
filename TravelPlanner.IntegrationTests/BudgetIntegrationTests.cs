using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelPlanner.Enums;
using TravelPlanner.Requests;
using TravelPlanner.Responses;

namespace TravelPlanner.IntegrationTests;

public class BudgetIntegrationTests
{
    [Fact]
    public async Task AddExpense_WhenCategoryLimitIsExceeded_ReturnsExceededBudgetStatus()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://localhost:7257");

        var cancellationToken = TestContext.Current.CancellationToken;
        var token = await RegisterTestUserAsync(client, cancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var tripDate = DateTime.Today.AddDays(1);
        var tripRequest = new CreateTripRequest(
            "Integration test trip",
            "Vilnius",
            tripDate,
            tripDate,
            "Integration Test User");

        using var tripResponse = await client.PostAsJsonAsync(
            "/api/Trip/create",
            tripRequest,
            cancellationToken);
        tripResponse.EnsureSuccessStatusCode();

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>(
            jsonOptions,
            cancellationToken);
        Assert.NotNull(trip);

        var inviteCode = trip.InviteCode;
        var participantId = Assert.Single(trip.Participants).Key;

        var limitsRequest = new SetBudgetLimitsRequest
        {
            TripCode = inviteCode,
            CategoryLimits = new Dictionary<string, decimal>
            {
                ["Food"] = 50m
            }
        };

        using var limitsResponse = await client.PostAsJsonAsync(
            "/api/Budget/limits",
            limitsRequest,
            cancellationToken);
        limitsResponse.EnsureSuccessStatusCode();

        var expenseRequest = new AddExpenseRequest
        {
            TripCode = inviteCode,
            ParticipantId = participantId,
            Category = "Food",
            Amount = 70m
        };

        using var expenseResponse = await client.PostAsJsonAsync(
            "/api/Budget/expense",
            expenseRequest,
            cancellationToken);
        expenseResponse.EnsureSuccessStatusCode();

        using var summaryResponse = await client.GetAsync(
            $"/api/Budget/summary?tripCode={Uri.EscapeDataString(inviteCode)}",
            cancellationToken);
        summaryResponse.EnsureSuccessStatusCode();

        var summary = await summaryResponse.Content.ReadFromJsonAsync<BudgetSummaryResponse>(
            jsonOptions,
            cancellationToken);
        Assert.NotNull(summary);

        var food = Assert.Single(summary.Results, result => result.Category == Category.Food);
        Assert.Equal(50m, food.Limit);
        Assert.Equal(70m, food.ActualSpent);
        Assert.Equal(BudgetStatus.Exceeded, food.Status);
        Assert.Equal(70m, summary.TotalSpent);
    }

    private static async Task<string> RegisterTestUserAsync(
        HttpClient client,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            email = $"integration-{Guid.NewGuid():N}@example.com",
            displayName = "Integration Test User",
            password = "IntegrationTest123!"
        };

        using var response = await client.PostAsJsonAsync(
            "/api/Auth/register",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        Assert.NotNull(auth);

        return auth.Token;
    }
}