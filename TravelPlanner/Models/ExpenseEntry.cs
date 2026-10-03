using System;

namespace TravelPlanner.Models
{
    public record ExpenseEntry(decimal Amount, Category Category, DateTime Date);
}