using System;
using System.Collections.Generic;

namespace TravelPlannerApp.Models
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