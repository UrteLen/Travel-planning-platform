using System;
using System.Collections.Generic;
using System.Linq;
using TravelPlanner.Models;

namespace TravelPlanner.Service
{
    public class TripVersionService
    {
        private readonly Dictionary<Guid, List<TripVersion>> _versionService = new();
        private readonly object _lock = new();

        public TripVersion SaveVersion(Guid tripId, Budget budget, List<PlannedVisit> plan, List<TripActivity> activities, string changedBy)
        {
            lock (_lock)
            {
                var list = GetList(tripId);
                var version = new TripVersion(list.Count + 1, DateTime.UtcNow, changedBy,
                    budget.Clone(), CopyPlan(plan), CopyActivities(activities), null);
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
                    restored.BudgetSnapshot.Clone(), CopyPlan(restored.PlanSnapshot),
                    CopyActivities(restored.ActivitiesSnapshot), versionNumber);
                list.Add(version);
                return version;
            }
        }

        public List<PlannedVisit> CopyPlan(IEnumerable<PlannedVisit> plan)
        {
            return plan.Select(v => v with { Place = v.Place.Clone() }).ToList();
        }

        public List<TripActivity> CopyActivities(IEnumerable<TripActivity> activities)
        {
            return activities.Select(a => a.Clone()).ToList();
        }

        private List<TripVersion> GetList(Guid tripId)
        {
            if (!_versionService.TryGetValue(tripId, out var list))
            {
                list = new List<TripVersion>();
                _versionService[tripId] = list;
            }
            return list;
        }
    }
}