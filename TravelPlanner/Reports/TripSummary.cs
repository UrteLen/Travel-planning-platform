using System;
using System.Collections.Generic;
using TravelPlanner.Responses;
using TravelPlanner.Models.Planning;

namespace TravelPlanner.Reports
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