namespace TravelPlanner.Models
{
    public record CategoryBudgetResult
    (
        Category Category,
        decimal Limit,
        decimal ActualSpent,
        BudgetStatus Status
    );
}