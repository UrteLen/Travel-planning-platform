using System;

namespace TravelPlannerApp.Models
{
    public record ExpenseEntry(decimal Amount, Category Category, DateTime Date);
}