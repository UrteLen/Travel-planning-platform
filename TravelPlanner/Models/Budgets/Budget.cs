using System.Collections.Generic;
using TravelPlanner.Enums;

namespace TravelPlanner.Models.Budgets
{
    public class Budget
    {
        public Dictionary<Category, decimal> CategoryLimits { get; set; } = new();
        public List<ExpenseEntry> Expenses { get; set; } = new();
    }
}    