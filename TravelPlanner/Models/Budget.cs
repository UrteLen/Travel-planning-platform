using TravelPlanner.Enums;

namespace TravelPlanner.Models;

public class Budget
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Dictionary<Category, decimal> CategoryLimits { get; set; } = new();
    public List<ExpenseEntry> Expenses { get; set; } = new();
}