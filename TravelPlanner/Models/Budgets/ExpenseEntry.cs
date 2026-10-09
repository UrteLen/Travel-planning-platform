using System;
using TravelPlanner.Models.Trips;
using TravelPlanner.Enums;

namespace TravelPlanner.Models.Budgets
{
    public record ExpenseEntry(decimal Amount, Category Category, DateTime Date, Participant Participant);
}