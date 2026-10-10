using System;
using System.Collections.Generic;
using System.Linq;
using TravelPlanner.Enums;
using TravelPlanner.Models.Budgets;
using TravelPlanner.Models.Planning;
using TravelPlanner.Models.History;

namespace TravelPlanner.Service
{
    public class PlanVersionService
    {
        private readonly Dictionary<Guid, List<TripVersion>> _versions = new();
        private readonly object _lock = new();

        public TripVersion SaveVersion(Guid tripId, Budget budget, List<PlannedVisit> plan, string changedBy)
        {
            lock (_lock)
            {
                var list = GetList(tripId);
                var version = new TripVersion(list.Count + 1, DateTime.UtcNow, changedBy,
                    budget.Clone(), CopyPlan(plan), null);
                list.Add(version);
                return version;
            }
        }

        public List<TripVersion> GetHistory(Guid tripId)
        {
            lock (_lock) { return new List<TripVersion>(GetList(tripId)); }
        }

        public TripVersion? GetVersion(Guid tripId, int versionNumber)
        {
            lock (_lock) { return GetList(tripId).FirstOrDefault(v => v.VersionNumber == versionNumber); }
        }

        public TripVersion Restore(Guid tripId, int versionNumber, string changedBy)
        {
            lock (_lock)
            {
                var list = GetList(tripId);
                var restored = list.FirstOrDefault(v => v.VersionNumber == versionNumber)
                    ?? throw new ArgumentException("Version not found");

                var version = new TripVersion(list.Count + 1, DateTime.UtcNow, changedBy,
                    restored.BudgetSnapshot.Clone(), CopyPlan(restored.PlanSnapshot), versionNumber);
                list.Add(version);
                return version;
            }
        }

        public List<PlannedVisit> CopyPlan(IEnumerable<PlannedVisit> plan)
        {
            return plan.Select(v => v with { Place = v.Place.Clone() }).ToList();
        }

        private List<TripVersion> GetList(Guid tripId)
        {
            if (!_versions.TryGetValue(tripId, out var list))
            {
                list = new List<TripVersion>();
                _versions[tripId] = list;
            }
            return list;
        }
    }
}