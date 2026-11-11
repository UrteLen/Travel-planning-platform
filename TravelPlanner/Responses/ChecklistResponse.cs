namespace TravelPlanner.Responses;

public record ChecklistResponse(
    List<ChecklistGroupResponse> Groups,
    int DoneCount,
    int TotalCount,
    int PercentDone);