using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Controllers;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ScheduleControllerTests
{
    [Fact]
    public async Task ScheduleActivityAsync_WhenServiceReportsOverlap_ReturnsConflict()
    {
        const string message = "Activities overlap.";
        var service = new ScheduleServiceStub(new InvalidOperationException(message));
        var controller = new ScheduleController(service);
        var tripId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var startTime = new DateTime(2026, 12, 1, 10, 0, 0);
        var request = new ScheduleActivityRequest(activityId, startTime, 40);

        var result = await controller.ScheduleActivityAsync(tripId, request);

        Assert.Equal(message, Assert.IsType<ConflictObjectResult>(result).Value);
        Assert.Equal((tripId, activityId, startTime, 40d), Assert.Single(service.Calls));
    }

    [Fact]
    public async Task ScheduleActivityAsync_WhenActivityIsMissing_ReturnsNotFound()
    {
        var service = new ScheduleServiceStub(new KeyNotFoundException());
        var controller = new ScheduleController(service);
        var request = new ScheduleActivityRequest(Guid.NewGuid(), new DateTime(2026, 12, 1), 40);

        var result = await controller.ScheduleActivityAsync(Guid.NewGuid(), request);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ScheduleActivityAsync_WhenCreated_ReturnsScheduledActivity()
    {
        var scheduled = CreateScheduledActivity();
        var service = new ScheduleServiceStub { Scheduled = scheduled };
        var request = new ScheduleActivityRequest(scheduled.Activity.Id, scheduled.StartTime, 40);

        var result = await new ScheduleController(service).ScheduleActivityAsync(Guid.NewGuid(), request);

        Assert.Same(scheduled, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Theory]
    [InlineData(false, "Speed must be positive.")]
    [InlineData(true, "Travel time is too large. Check the travel speed.")]
    public async Task ScheduleActivityAsync_WhenTravelInputIsRejected_ReturnsBadRequest(
        bool overflow,
        string expectedMessage)
    {
        Exception failure = overflow ? new OverflowException() : new ArgumentException(expectedMessage);
        var service = new ScheduleServiceStub(failure);
        var request = new ScheduleActivityRequest(Guid.NewGuid(), new DateTime(2026, 12, 1), 0);

        var result = await new ScheduleController(service).ScheduleActivityAsync(Guid.NewGuid(), request);

        Assert.Equal(expectedMessage, Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetDailyScheduleAsync_WhenTripExists_ReturnsScheduleAndPassesRequestedDate()
    {
        var tripId = Guid.NewGuid();
        var scheduled = CreateScheduledActivity();
        var date = scheduled.StartTime.Date;
        var service = new ScheduleServiceStub { DailySchedule = [scheduled] };

        var result = await new ScheduleController(service).GetDailyScheduleAsync(tripId, date);

        Assert.Same(service.DailySchedule, Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal((tripId, (DateTime?)date), service.ScheduleQuery);
    }

    [Fact]
    public async Task GetDailyScheduleAsync_WhenTripIsMissing_ReturnsNotFound()
    {
        var result = await new ScheduleController(new ScheduleServiceStub())
            .GetDailyScheduleAsync(Guid.NewGuid(), null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RemoveScheduledActivityAsync_WhenRemoved_ReturnsNoContent()
    {
        var tripId = Guid.NewGuid();
        var scheduledId = Guid.NewGuid();
        var service = new ScheduleServiceStub();

        var result = await new ScheduleController(service).RemoveScheduledActivityAsync(tripId, scheduledId);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal((tripId, scheduledId), service.Removal);
    }

    [Fact]
    public async Task RemoveScheduledActivityAsync_WhenEntryIsMissing_ReturnsNotFound()
    {
        const string message = "Scheduled activity not found.";
        var service = new ScheduleServiceStub(new KeyNotFoundException(message));

        var result = await new ScheduleController(service)
            .RemoveScheduledActivityAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(message, Assert.IsType<NotFoundObjectResult>(result).Value);
    }

    [Fact]
    public async Task UpdateScheduledActivityAsync_WhenUpdated_ReturnsEntryAndPassesNewTime()
    {
        var tripId = Guid.NewGuid();
        var scheduled = CreateScheduledActivity();
        var service = new ScheduleServiceStub { Scheduled = scheduled };
        var request = new UpdateScheduledActivityRequest(scheduled.StartTime, 40);

        var result = await new ScheduleController(service)
            .UpdateScheduledActivityAsync(tripId, scheduled.Id, request);

        Assert.Same(scheduled, Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal((tripId, scheduled.Id, scheduled.StartTime, 40d), service.Update);
    }

    [Theory]
    [InlineData(typeof(KeyNotFoundException), typeof(NotFoundObjectResult))]
    [InlineData(typeof(InvalidOperationException), typeof(ConflictObjectResult))]
    [InlineData(typeof(ArgumentException), typeof(BadRequestObjectResult))]
    [InlineData(typeof(OverflowException), typeof(BadRequestObjectResult))]
    public async Task UpdateScheduledActivityAsync_WhenServiceRejectsUpdate_ReturnsExpectedResult(
        Type exceptionType,
        Type expectedResultType)
    {
        var failure = Assert.IsAssignableFrom<Exception>(Activator.CreateInstance(exceptionType));
        var service = new ScheduleServiceStub(failure);
        var request = new UpdateScheduledActivityRequest(new DateTime(2026, 12, 1), 40);

        var result = await new ScheduleController(service)
            .UpdateScheduledActivityAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        Assert.IsType(expectedResultType, result);
    }

    private static ScheduledActivity CreateScheduledActivity()
    {
        var activity = new TripActivity(
            "Museum",
            Category.Activities,
            new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1));

        return new ScheduledActivity(activity, new DateTime(2026, 12, 1, 10, 0, 0));
    }

    private sealed class ScheduleServiceStub(Exception? failure = null) : IActivityScheduleService
    {
        public List<(Guid TripId, Guid ActivityId, DateTime StartTime, double SpeedKmh)> Calls { get; } = [];

        public ScheduledActivity? Scheduled { get; init; }
        public List<ScheduledActivity>? DailySchedule { get; init; }
        public (Guid TripId, DateTime? Date)? ScheduleQuery { get; private set; }
        public (Guid TripId, Guid ScheduledId)? Removal { get; private set; }
        public (Guid TripId, Guid ScheduledId, DateTime Start, double Speed)? Update { get; private set; }

        public Task<ScheduledActivity> ScheduleActivityAsync(
            Guid tripId,
            Guid activityId,
            DateTime startTime,
            double speedKmh)
        {
            Calls.Add((tripId, activityId, startTime, speedKmh));
            return WriteResult();
        }

        public Task<List<ScheduledActivity>?> GetScheduleAsync(Guid tripId, DateTime? date)
        {
            ScheduleQuery = (tripId, date);
            return Task.FromResult(DailySchedule);
        }

        public Task RemoveScheduledActivityAsync(Guid tripId, Guid scheduledActivityId)
        {
            Removal = (tripId, scheduledActivityId);
            return failure is null ? Task.CompletedTask : Task.FromException(failure);
        }

        public Task<ScheduledActivity> UpdateScheduledActivityAsync(
            Guid tripId,
            Guid scheduledActivityId,
            DateTime startTime,
            double speedKmh)
        {
            Update = (tripId, scheduledActivityId, startTime, speedKmh);
            return WriteResult();
        }

        private Task<ScheduledActivity> WriteResult()
        {
            return failure is null
                ? Task.FromResult(Scheduled ?? throw new InvalidOperationException("Configure a scheduled result."))
                : Task.FromException<ScheduledActivity>(failure);
        }
    }
}
