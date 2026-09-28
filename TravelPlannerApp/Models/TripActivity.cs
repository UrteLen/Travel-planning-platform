namespace TravelPlannerApp.Models;

public class TripActivity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Category Category { get; set; } = Category.Activities;
    public GeoLocation Location { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal EstimatedCost { get; set; }
    public bool IsOptional { get; set; }

    public TripActivity(
    string name,
    Category category,
    GeoLocation location,
    TimeSpan duration,
    decimal estimatedCost = 0m,
    bool optional = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Activity name cannot be empty.", nameof(name));
        }

        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Activity duration must be greater than zero.",
                nameof(duration));
        }

        if (estimatedCost < 0)
        {
            throw new ArgumentException(
                "Estimated cost cannot be negative.",
                nameof(estimatedCost));
        }
        
        Name = name;
        Category = category;
        Location = location;
        Duration = duration;
        EstimatedCost = estimatedCost;
        IsOptional = optional;
    }
}