using System.Text;
using TravelPlanner.Enums;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class ActivityImportServiceTests
{
    [Fact]
    public async Task LoadActivitiesFromJsonAsync_WhenJsonContainsOneValidActivity_ReturnsMappedActivity()
    {
        var json = """
                   [
                       {
                           "name": "Restaurant",
                           "category": "Food",
                           "latitude": 54.6872,
                           "longitude": 25.2797,
                           "durationHours": 1,
                           "estimatedCost": 12
                       }
                   ]
                   """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var service = new ActivityImportService();
        var activities = await service.LoadActivitiesFromJsonAsync(stream);

        var activity = Assert.Single(activities);
        Assert.Equal("Restaurant", activity.Name);
        Assert.Equal(Category.Food, activity.Category);
        Assert.Equal(TimeSpan.FromHours(1), activity.Duration);
        Assert.Equal(12m, activity.EstimatedCost);
        Assert.Equal(54.6872, activity.Location.Latitude);
        Assert.Equal(25.2797, activity.Location.Longitude);
    }

    [Fact]
    public async Task LoadActivitiesFromJsonAsync_WhenLongitudeIsMissing_ThrowsArgumentException()
    {
        var json = """
                   [
                       {
                           "name": "Restaurant",
                           "category": "Food",
                           "latitude": 54.6872,
                           "durationHours": 1,
                           "estimatedCost": 12
                       }
                   ]
                   """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var service = new ActivityImportService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.LoadActivitiesFromJsonAsync(stream));

        Assert.Equal("Latitude and longitude are required.", exception.Message);
    }

    [Fact]
    public async Task LoadActivitiesFromJsonAsync_WhenDurationIsZero_ThrowsArgumentException()
    {
        var json = """
                   [
                       {
                           "name": "Restaurant",
                           "category": "Food",
                           "latitude": 54.6872,
                           "longitude": 25.2797,
                           "durationHours": 0,
                           "estimatedCost": 12
                       }
                   ]
                   """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var service = new ActivityImportService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.LoadActivitiesFromJsonAsync(stream));

        Assert.Equal("Invalid duration.", exception.Message);
    }

    [Fact]
    public async Task LoadActivitiesFromJsonAsync_WhenCategoryIsUndefined_ThrowsArgumentException()
    {
        var json = """
                   [
                       {
                           "name": "Restaurant",
                           "category": "999",
                           "latitude": 54.6872,
                           "longitude": 25.2797,
                           "durationHours": 1,
                           "estimatedCost": 12
                       }
                   ]
                   """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var service = new ActivityImportService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.LoadActivitiesFromJsonAsync(stream));

        Assert.Equal("Invalid category.", exception.Message);
    }
}