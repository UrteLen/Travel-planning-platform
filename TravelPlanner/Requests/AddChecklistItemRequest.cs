using TravelPlanner.Enums;

namespace TravelPlanner.Requests;

public record AddChecklistItemRequest(
    string Text,
    ChecklistCategory Category = ChecklistCategory.Other);