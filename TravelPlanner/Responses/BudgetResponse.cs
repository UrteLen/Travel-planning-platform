using TravelPlanner.Enums;

namespace TravelPlanner.Responses;

public record BudgetResponse(
    Dictionary<Category, decimal> CategoryLimits,
    List<ExpenseResponse> Expenses);