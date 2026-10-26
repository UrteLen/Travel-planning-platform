using TravelPlanner.Enums;

namespace TravelPlanner.Models;

public class TripActivity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Category Category { get; set; } = Category.Activities;
    public GeoLocation Location { get; set; }
    public TimeSpan Duration { get; set; }
    public decimal EstimatedCost { get; set; }
    public bool IsOptional { get; set; }
    public TimeSpan? OpeningTime { get; set; }
    public TimeSpan? ClosingTime { get; set; }
    public Dictionary<Guid, VoteType> Votes { get; set; } = new();

    public TripActivity(
        string name,
        Category category,
        GeoLocation location,
        TimeSpan duration,
        decimal estimatedCost = 0m,
        bool optional = true,
        TimeSpan? openingTime = null,
        TimeSpan? closingTime = null)
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

        if (openingTime.HasValue != closingTime.HasValue)
        {
            throw new ArgumentException(
                "Provide both opening and closing times, or neither.");
        }

        if (openingTime.HasValue &&
            (openingTime.Value < TimeSpan.Zero ||
             openingTime.Value >= TimeSpan.FromDays(1)))
        {
            throw new ArgumentException(
                "Opening time must be between 00:00 and less than 24:00.",
                nameof(openingTime));
        }

        if (closingTime.HasValue &&
            (closingTime.Value < TimeSpan.Zero ||
             closingTime.Value >= TimeSpan.FromDays(1)))
        {
            throw new ArgumentException(
                "Closing time must be between 00:00 and less than 24:00.",
                nameof(closingTime));
        }

        Name = name;
        Category = category;
        Location = location;
        Duration = duration;
        EstimatedCost = estimatedCost;
        IsOptional = optional;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
    }

    public TripActivity Clone()
    {
        return new TripActivity(
            Name,
            Category,
            Location,
            Duration,
            EstimatedCost,
            IsOptional)
        {
            Id = Id,
            OpeningTime = OpeningTime,
            ClosingTime = ClosingTime,
            Votes = new Dictionary<Guid, VoteType>(Votes)
        };
    }
    private TripActivity()
    {
    }
}
