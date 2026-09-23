using System.Collections.Generic;
namespace TravelPlannerApp.Models
{
    public class Budget
    {
        public Dictionary<Category, decimal> CategoryLimits { get; set; } = new();
        public List<ExpenseEntry> Expenses { get; set; } = new();
        public int NumberOfParticipants { get; set; }
    }
}    