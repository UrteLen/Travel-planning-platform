namespace TravelPlanner.Responses;

public record BudgetSummaryResponse(
    List<CategoryBudgetResult> Results,
    decimal CostPerPerson);