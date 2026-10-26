using TravelPlanner.Enums;

namespace TravelPlanner.Models;

public record ExpenseEntry(decimal Amount, Category Category, DateTime Date, Participant Participant)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    private ExpenseEntry() : this(0m, default, default, null!)
    {
    }
}