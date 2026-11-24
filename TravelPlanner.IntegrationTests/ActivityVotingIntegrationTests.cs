using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelPlanner.Enums;
using TravelPlanner.Requests;
using TravelPlanner.Responses;

namespace TravelPlanner.IntegrationTests;

public class ActivityVotingIntegrationTests
{
    [Fact]
    public async Task ActivityVotes_WhenChangedAndRemoved_PersistResultsThroughTheApi()
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

        var participantId = Assert.Single(trip.Participants).Key;
        var votesUrl = $"/api/trips/{tripId}/activities/{activity.Id}/votes";
        var participantVotesUrl = $"{votesUrl}/{participantId}";

        using var upvoteResponse = await client.PutAsJsonAsync(
            participantVotesUrl,
            new VoteRequest(VoteType.Upvote),
            cancellationToken);
        upvoteResponse.EnsureSuccessStatusCode();

        var upvotes = await client.GetFromJsonAsync<VoteResultsResponse>(
            $"{votesUrl}/results?participantId={participantId}", jsonOptions, cancellationToken);
        Assert.NotNull(upvotes);
        Assert.Equal(1, upvotes.Upvotes);
        Assert.Equal(0, upvotes.Downvotes);
        Assert.Equal(VoteType.Upvote, upvotes.ParticipantVote);

        using var downvoteResponse = await client.PutAsJsonAsync(
            participantVotesUrl,
            new VoteRequest(VoteType.Downvote),
            cancellationToken);
        downvoteResponse.EnsureSuccessStatusCode();

        var downvotes = await client.GetFromJsonAsync<VoteResultsResponse>(
            $"{votesUrl}/results?participantId={participantId}", jsonOptions, cancellationToken);
        Assert.NotNull(downvotes);
        Assert.Equal(0, downvotes.Upvotes);
        Assert.Equal(1, downvotes.Downvotes);
        Assert.Equal(VoteType.Downvote, downvotes.ParticipantVote);

        using var deleteVoteResponse = await client.DeleteAsync(participantVotesUrl, cancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteVoteResponse.StatusCode);

        var clearedVotes = await client.GetFromJsonAsync<VoteResultsResponse>(
            $"{votesUrl}/results?participantId={participantId}", jsonOptions, cancellationToken);
        Assert.NotNull(clearedVotes);
        Assert.Equal(0, clearedVotes.Upvotes);
        Assert.Equal(0, clearedVotes.Downvotes);
        Assert.Null(clearedVotes.ParticipantVote);

        var prioritized = await client.GetFromJsonAsync<ActivityResponse[]>(
            $"/api/trips/{tripId}/activities/prioritized", jsonOptions, cancellationToken);
        Assert.NotNull(prioritized);
        Assert.Equal(activity.Id, Assert.Single(prioritized).Id);

        var selected = await client.GetFromJsonAsync<ActivityResponse[]>(
            $"/api/trips/{tripId}/activities/selected?budget=0&hours=2", jsonOptions, cancellationToken);
        Assert.NotNull(selected);
        Assert.Equal(activity.Id, Assert.Single(selected).Id);
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
