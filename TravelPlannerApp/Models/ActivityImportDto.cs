namespace TravelPlannerApp.Models;

public class ActivityImportDto
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double DurationHours { get; set; }
    public decimal EstimatedCost { get; set; }

    public bool Optional { get; set; } = true;
}