using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record ChecklistItemResponse(Guid Id, string Text, ChecklistCategory Category, bool IsDone);