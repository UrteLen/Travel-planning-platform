using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanner.Controllers;
using TravelPlanner.Data;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ActivityControllerTests
{
    [Theory]
    [InlineData("Unknown", 1, "Invalid category.")]
    [InlineData("Activities", 0, "Activity duration must be a finite number greater than zero.")]
    public async Task AddAsync_WhenInputIsInvalid_ReturnsBadRequest(
        string category,
        double durationHours,
        string expectedMessage)
    {
        await using var db = CreateUnconfiguredDbContext();
        var controller = CreateController(db);
        var request = new AddActivityRequest(
            Name: "Museum",
            Category: category,
            Latitude: 54.6872,
            Longitude: 25.2797,
            DurationHours: durationHours);

        var result = await controller.AddAsync(Guid.NewGuid(), request);

        Assert.Equal(expectedMessage, Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    [Fact]
    public async Task ImportAsync_WhenFileIsEmpty_ReturnsBadRequestWithoutOpeningFile()
    {
        await using var db = CreateUnconfiguredDbContext();
        var controller = CreateController(db);
        var file = new EmptyUpload();

        var result = await controller.ImportAsync(Guid.NewGuid(), file);

        Assert.Equal("File is empty.", Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.False(file.WasOpened);
    }

    [Fact]
    public async Task ImportAsync_WhenJsonIsMalformed_ReturnsBadRequest()
    {
        await using var db = CreateUnconfiguredDbContext();
        var controller = CreateController(db);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("[invalid"));
        var file = new FormFile(stream, 0, stream.Length, "file", "activities.json");

        var result = await controller.ImportAsync(Guid.NewGuid(), file);

        Assert.Equal("Invalid JSON file.", Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    [Fact]
    public async Task AddAsync_WhenCreated_ReturnsMappedActivity()
    {
        var activity = CreateActivity();
        var controller = new ActivityController(new ActivityServiceStub { Activity = activity });
        var request = new AddActivityRequest("Museum", "Activities", 54.6872, 25.2797, 1);

        var result = await controller.AddAsync(Guid.NewGuid(), request);

        var response = Assert.IsType<ActivityResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(activity.Id, response.Id);
        Assert.Equal(activity.Name, response.Name);
        Assert.Equal(activity.EstimatedCost, response.EstimatedCost);
    }

    [Fact]
    public async Task ImportAsync_WhenServiceImportsActivity_ReturnsMappedList()
    {
        var activity = CreateActivity();
        var controller = new ActivityController(new ActivityServiceStub { Activities = [activity] });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("[]"));
        var file = new FormFile(stream, 0, stream.Length, "file", "activities.json");

        var result = await controller.ImportAsync(Guid.NewGuid(), file);

        var activities = Assert.IsType<List<ActivityResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(activity.Id, Assert.Single(activities).Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetActivities_WhenTripExists_ReturnsMappedList(bool prioritized)
    {
        var activity = CreateActivity();
        var controller = new ActivityController(new ActivityServiceStub { Activities = [activity] });
        var tripId = Guid.NewGuid();

        var result = prioritized
            ? await controller.GetPrioritizedActivitiesAsync(tripId)
            : await controller.GetSelectedActivitiesAsync(tripId, 20m, 2);

        var activities = Assert.IsType<List<ActivityResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(activity.Id, Assert.Single(activities).Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetActivities_WhenTripIsMissing_ReturnsNotFound(bool prioritized)
    {
        var controller = new ActivityController(new ActivityServiceStub());
        var tripId = Guid.NewGuid();

        var result = prioritized
            ? await controller.GetPrioritizedActivitiesAsync(tripId)
            : await controller.GetSelectedActivitiesAsync(tripId, 20m, 2);

        Assert.Equal("Trip not found.", Assert.IsType<NotFoundObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetSelectedActivitiesAsync_WhenBudgetIsRejected_ReturnsBadRequest()
    {
        const string message = "Budget must be non-negative.";
        var controller = new ActivityController(new ActivityServiceStub { Error = message });

        var result = await controller.GetSelectedActivitiesAsync(Guid.NewGuid(), -1m, 2);

        Assert.Equal(message, Assert.IsType<BadRequestObjectResult>(result).Value);
    }

    private static TripActivity CreateActivity()
    {
        return new TripActivity(
            "Museum", Category.Activities, new GeoLocation(54.6872, 25.2797),
            TimeSpan.FromHours(1), estimatedCost: 12m);
    }

    private static TravelPlannerDbContext CreateUnconfiguredDbContext()
    {
        return new TravelPlannerDbContext(
            new DbContextOptionsBuilder<TravelPlannerDbContext>().Options);
    }

    private static ActivityController CreateController(TravelPlannerDbContext db)
    {
        var service = new ActivityManagementService(
            db,
            new ActivityService(),
            new ActivityImportService(),
            new ActivityVotingService(),
            new ActivitySchedulingService());

        return new ActivityController(service);
    }

    private sealed class EmptyUpload : IFormFile
    {
        public long Length => 0;
        public bool WasOpened { get; private set; }
        public string ContentType => "application/json";
        public string ContentDisposition => string.Empty;
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public string Name => "file";
        public string FileName => "activities.json";

        public Stream OpenReadStream()
        {
            WasOpened = true;
            return new MemoryStream();
        }

        public void CopyTo(Stream target) => throw new NotSupportedException();

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ActivityServiceStub : ITripActivityService
    {
        public TripActivity? Activity { get; init; }
        public List<TripActivity>? Activities { get; init; }
        public string? Error { get; init; }

        public Task<TripActivity?> AddActivityAsync(
            Guid tripId,
            string name,
            string category,
            double latitude,
            double longitude,
            double durationHours,
            decimal estimatedCost = 0m,
            bool optional = true,
            TimeSpan? openingTime = null,
            TimeSpan? closingTime = null)
            => Task.FromResult(Activity);

        public Task<List<TripActivity>?> GetPrioritizedActivitiesAsync(Guid tripId)
            => Task.FromResult(Activities);

        public Task<(List<TripActivity>? Activities, string? ValidationError)> GetSelectedActivitiesAsync(
            Guid tripId,
            decimal? budget,
            double? hours)
            => Task.FromResult((Activities, Error));

        public Task<List<TripActivity>?> ImportActivitiesAsync(
            Guid tripId,
            Stream stream)
            => Task.FromResult(Activities);
    }
}
