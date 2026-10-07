using System;
using System.Collections.Generic;

namespace TravelPlanner.Models
{
    public record TripSummary(
        decimal TotalPlannedBudget,
        decimal TotalActualSpend,
        decimal CostPerPerson,
        List<CategoryBudgetResult> CategoryBreakdown,
        List<PlannedVisit> Itinerary,
        DateTime GeneratedAt
    );
}