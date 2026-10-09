namespace TravelPlanner.Responses;

public record SettlementResponse(
    string From,
    string To,
    decimal Amount);