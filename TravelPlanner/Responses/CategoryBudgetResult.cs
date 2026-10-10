using TravelPlanner.Enums;

namespace TravelPlanner.Responses
{
    public record CategoryBudgetResult
    (
        Category Category,
        decimal Limit,
        decimal ActualSpent,
        BudgetStatus Status
    );
}