using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record ParticipantResponse(
    Guid Id,
    string Name,
    UserRole Role
);