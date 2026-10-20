using System;
using System.Collections.Generic;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Reports;

public record TripSummary(
    decimal TotalPlannedBudget,
    decimal TotalActualSpend,
    decimal CostPerPerson,
    List<CategoryBudgetResult> CategoryBreakdown,
    List<PlannedVisit> Itinerary,
    DateTime GeneratedAt
);