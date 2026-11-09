namespace TravelPlanner.Responses;

public record AuthResponse(string Token, Guid UserId, string DisplayName);