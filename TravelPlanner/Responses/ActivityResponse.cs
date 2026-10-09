using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record ActivityResponse(
    Guid Id,
    string Name,
    Category Category,
    GeoLocationResponse Location,
    TimeSpan Duration,
    decimal EstimatedCost,
    bool IsOptional,
    TimeSpan? OpeningTime,
    TimeSpan? ClosingTime);