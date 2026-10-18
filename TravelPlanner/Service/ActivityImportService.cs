using System.Text.Json;
using TravelPlanner.Imports;
using TravelPlanner.Enums;
using TravelPlanner.Models;

namespace TravelPlanner.Service;

public class ActivityImportService
{
    public List<ActivityImportData> LoadFromJson(Stream stream)
    {
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
            if (dto is null)
            {
                throw new ArgumentException("Activity cannot be null.");
            }

            if (!Enum.TryParse<Category>(dto.Category, true, out var category)
                || !Enum.IsDefined(category))
            {
                throw new ArgumentException("Invalid category.");
            }

            var location = new GeoLocation(dto.Latitude, dto.Longitude);

            if (!double.IsFinite(dto.DurationHours) || dto.DurationHours <= 0)
            {
                throw new ArgumentException("Invalid duration.");
            }

            var duration = TimeSpan.FromHours(dto.DurationHours);

            var activity = new TripActivity(
                dto.Name,
                category,
                location,
                duration,
                dto.EstimatedCost,
                dto.Optional,
                dto.OpeningTime,
                dto.ClosingTime);

            activities.Add(activity);
        }

        return activities;
    }

    public List<TripActivity> LoadActivitiesFromJson(Stream stream)
    {
        var importedActivities = LoadFromJson(stream);

        return ConvertToActivities(importedActivities);
    }
}