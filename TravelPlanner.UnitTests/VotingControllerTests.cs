using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Controllers;
using TravelPlanner.Enums;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class VotingControllerTests
{
    [Fact]
    public async Task VoteAsync_WhenVoteTypeIsMissing_ReturnsBadRequest()
    {
        var controller = new VotingController(new VotingServiceStub(new InvalidOperationException()));

        var result = await controller.VoteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new VoteRequest(null));

        Assert.Equal("Vote type is required.", Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    [Fact]
    public async Task VoteAsync_WhenVoteIsRecorded_ReturnsOk()
    {
        var result = await new VotingController(new VotingServiceStub()).VoteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new VoteRequest(VoteType.Upvote));

        Assert.IsType<OkResult>(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VoteAsync_WhenServiceRejectsVote_ReturnsExpectedError(bool missing)
    {
        const string message = "Vote could not be recorded.";
        Exception failure = missing ? new KeyNotFoundException(message) : new ArgumentException(message);
        var controller = new VotingController(new VotingServiceStub(failure));

        var result = await controller.VoteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new VoteRequest(VoteType.Upvote));

        var response = missing
            ? (ObjectResult)Assert.IsType<NotFoundObjectResult>(result)
            : Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(message, response.Value);
    }

    [Fact]
    public async Task GetVoteResultsAsync_WhenFound_ReturnsResults()
    {
        var votes = new VoteResultsResponse(2, 1, VoteType.Upvote);
        var controller = new VotingController(new VotingServiceStub { Results = votes });

        var result = await controller.GetVoteResultsAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(votes, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetVoteResultsAsync_WhenMissing_ReturnsNotFound()
    {
        const string message = "Activity not found.";
        var controller = new VotingController(new VotingServiceStub { Error = message });

        var result = await controller.GetVoteResultsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(message, Assert.IsType<NotFoundObjectResult>(result).Value);
    }

    [Fact]
    public async Task RemoveVoteAsync_WhenRemoved_ReturnsNoContent()
    {
        var result = await new VotingController(new VotingServiceStub()).RemoveVoteAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoveVoteAsync_WhenActivityIsMissing_ReturnsNotFound()
    {
        const string message = "Activity not found.";
        var controller = new VotingController(new VotingServiceStub(new KeyNotFoundException(message)));

        var result = await controller.RemoveVoteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(message, Assert.IsType<NotFoundObjectResult>(result).Value);
    }

    private sealed class VotingServiceStub(Exception? failure = null) : IActivityVotingService
    {
        public VoteResultsResponse? Results { get; init; }
        public string? Error { get; init; }

        public Task<(VoteResultsResponse? Results, string? NotFoundError)> GetVoteResultsAsync(
            Guid tripId,
            Guid activityId,
            Guid? participantId)
            => Task.FromResult((Results, Error));

        public Task VoteAsync(
            Guid tripId,
            Guid participantId,
            Guid activityId,
            VoteType voteType)
            => failure is null ? Task.CompletedTask : Task.FromException(failure);

        public Task RemoveVoteAsync(
            Guid tripId,
            Guid participantId,
            Guid activityId)
            => failure is null ? Task.CompletedTask : Task.FromException(failure);
    }
}
