using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelPlanner.Requests;
using TravelPlanner.Responses;

namespace TravelPlanner.IntegrationTests;

public class ActivitySchedulingIntegrationTests
{
    [Fact]
    public async Task ScheduleActivity_WhenScheduled_AppearsInDailySchedule()
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

        var tripId = trip.Id;

        var activityRequest = new AddActivityRequest(
            Name: "Museum",
            Category: "Activities",
            Latitude: 54.6872,
            Longitude: 25.2797,
            DurationHours: 1);

        using var activityResponse = await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/activities",
            activityRequest,
            cancellationToken);
        activityResponse.EnsureSuccessStatusCode();

        var activity = await activityResponse.Content.ReadFromJsonAsync<ActivityResponse>(
            jsonOptions,
            cancellationToken);
        Assert.NotNull(activity);

        var startTime = tripDate.AddHours(10);
        var scheduleRequest = new ScheduleActivityRequest(activity.Id, startTime, 40);

        using var scheduleResponse = await client.PostAsJsonAsync(
            $"/api/trips/{tripId}/scheduled-activities",
            scheduleRequest,
            cancellationToken);
        scheduleResponse.EnsureSuccessStatusCode();

        var date = tripDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var dailyScheduleResponse = await client.GetAsync(
            $"/api/trips/{tripId}/scheduled-activities?date={date}",
            cancellationToken);
        dailyScheduleResponse.EnsureSuccessStatusCode();

        var dailySchedule = await dailyScheduleResponse.Content.ReadFromJsonAsync<JsonElement[]>(
            cancellationToken);

        Assert.NotNull(dailySchedule);
        var scheduledActivity = Assert.Single(dailySchedule);
        Assert.Equal(activity.Id, scheduledActivity.GetProperty("activity").GetProperty("id").GetGuid());
        Assert.Equal(startTime, scheduledActivity.GetProperty("startTime").GetDateTime());
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