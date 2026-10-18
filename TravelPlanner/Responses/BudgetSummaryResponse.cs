namespace TravelPlanner.Responses;
public record BudgetSummaryResponse(
    List<CategoryBudgetResult> Results,
    decimal CostPerPerson,
    decimal TotalSpent);
    var summary = new BudgetSummaryResponse(results, costPerPerson, totalSpent);