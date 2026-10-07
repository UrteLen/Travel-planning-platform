using System;
using System.Collections.Generic;

namespace TravelPlanner.Models
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