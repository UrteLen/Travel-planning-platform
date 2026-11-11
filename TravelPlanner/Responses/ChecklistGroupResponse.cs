using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record ChecklistGroupResponse(ChecklistCategory Category, List<ChecklistItemResponse> Items);