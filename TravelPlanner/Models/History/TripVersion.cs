using System;
using System.Collections.Generic;
using TravelPlanner.Models.Budgets;
using TravelPlanner.Models.Planning;

namespace TravelPlanner.Models.History
{
    public record TripVersion(
        int VersionNumber,
        DateTime Timestamp,
        string ChangedBy,
        Budget BudgetSnapshot,
        List<PlannedVisit> PlanSnapshot,
        int? RestoredFromVersion
    );
}