using System.Text.Json;
using TravelPlanner.Models;
using TravelPlanner.Imports;

namespace TravelPlanner.Service;

public class ActivityImportService
{
    public List<ActivityImportData> LoadFromJson(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var activities = JsonSerializer.Deserialize<List<ActivityImportData>>(stream, options);
        return activities ?? new List<ActivityImportData>();
    }

    public List<TripActivity> ConvertToActivities(IEnumerable<ActivityImportData> importedActivities)
    {
        var activities = new List<TripActivity>();

        foreach (var dto in importedActivities)
        {
            var category = Enum.Parse<Category>(dto.Category, ignoreCase: true);
            var location = new GeoLocation(dto.Latitude, dto.Longitude);
            var duration = TimeSpan.FromHours(dto.DurationHours);
            var activity = new TripActivity(dto.Name, category, location, duration, dto.EstimatedCost, dto.Optional);

            activities.Add(activity);
        }

        return activities;
    }

    public List<TripActivity> LoadActivitiesFromJson(string filePath)
    {
        var importedActivities = LoadFromJson(filePath);

        return ConvertToActivities(importedActivities);
    }
}