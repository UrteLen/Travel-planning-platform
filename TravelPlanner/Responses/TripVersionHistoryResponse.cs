namespace TravelPlanner.Responses;

public record TripVersionHistoryResponse(
    int VersionNumber,
    DateTime Timestamp,
    string ChangedBy,
    int? RestoredFromVersion);