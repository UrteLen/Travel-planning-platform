using TravelPlanner.Enums;

namespace TravelPlanner.Models
{
    public record ExpenseEntry(decimal Amount, Category Category, DateTime Date, Participant Participant);
}