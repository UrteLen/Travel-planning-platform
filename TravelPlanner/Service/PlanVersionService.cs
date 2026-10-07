using System;
using System.Collections.Generic;
using System.Linq;
using TravelPlanner.Models;

namespace TravelPlanner.Service
{
    public class PlanVersionService
    {
        private readonly List<TripVersion>_versions = new();

        public TripVersion SaveVersion(Budget budget, List<PlannedVisit> plan, string changedBy)
        {
            int nextVersionNumber = _versions.Count + 1;
            DateTime timestamp = DateTime.Now;

            var budgetCopy = new Budget
            {
                CategoryLimits = new Dictionary<Category, decimal>(budget.CategoryLimits),
                Expenses = budget.Expenses.Select(e => e with { }).ToList()
            };

            var planCopy = plan.Select(p => p with { }).ToList();

            var version = new TripVersion(nextVersionNumber, timestamp, changedBy, budgetCopy, planCopy, null);
            _versions.Add(version);

            return version;
        }

        public IReadOnlyList<TripVersion> GetHistory()
        {
            return _versions.AsReadOnly();
        }
        public TripVersion? GetVersion(int versionNumber)
        {
            return _versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
        }

        public TripVersion Restore(int versionNumber, string changedBy)
       {
            TripVersion? restoredVersion = GetVersion(versionNumber);

            if (restoredVersion == null)
            {
                throw new ArgumentException("Version not found");
            }

            int nextVersionNumber = _versions.Count + 1;
            DateTime timestamp = DateTime.Now;

            var version = new TripVersion(nextVersionNumber, timestamp, changedBy, restoredVersion.BudgetSnapshot, restoredVersion.PlanSnapshot, versionNumber);
            _versions.Add(version);

            return version;
        }
    }
}