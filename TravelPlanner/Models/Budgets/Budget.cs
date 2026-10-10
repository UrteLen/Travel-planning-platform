using System.Collections.Generic;
using TravelPlanner.Enums;
using System.Linq;

namespace TravelPlanner.Models.Budgets
{
    public class Budget
    {
        public Dictionary<Category, decimal> CategoryLimits { get; set; } = new();
        public List<ExpenseEntry> Expenses { get; set; } = new();

        public Budget Clone() => new()
        {
            CategoryLimits = new Dictionary<Category, decimal>(CategoryLimits),
            Expenses = Expenses.Select(e => e with { Participant = e.Participant.Clone()}).ToList()
        };
    }
}    