using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public class PlanService
{
    private readonly DayPlanningService _dayPlanningService;
    private readonly TripVersionService _versionService;

    public PlanService(
        DayPlanningService dayPlanningService,
        TripVersionService versionService)
    {
        _dayPlanningService = dayPlanningService;
        _versionService = versionService;
    }

    public List<PlannedVisit> GeneratePlan(Trip trip, GeoLocation start, DateTime startTime, double speedKmh, string changedBy)
    {
        var places = trip.Activities.Select(ToPlace).ToList();
        trip.Plan = _dayPlanningService.DayPlan(places, start, startTime, speedKmh);
        _versionService.SaveVersion(trip.Id, trip.Budget, trip.Plan, trip.Activities, changedBy);

        return trip.Plan;
    }

    public TripVersion RestoreVersion(Trip trip, int versionNumber, string changedBy)
    {
        var version = _versionService.Restore(trip.Id, versionNumber, changedBy);

        trip.Plan = version.PlanSnapshot
            .Select(visit => new PlannedVisit(visit.Place.Clone(), visit.ArrivalTime, visit.DepartureTime))
            .ToList();

        RestoreBudget(trip, version.BudgetSnapshot);
        RestoreActivities(trip, version.ActivitiesSnapshot);

        return version;
    }

    public static List<PlannedVisitResponse> ToResponseList(List<PlannedVisit> plan)
    {
        return plan.Order().Select(v => new PlannedVisitResponse(v.Place.Name, v.ArrivalTime, v.DepartureTime)).ToList();
    }

    // Updates the trip's existing budget instead of replacing it,
    // because EF Core is already tracking it and its participants.
    private static void RestoreBudget(Trip trip, Budget snapshot)
    {
        trip.Budget.CategoryLimits = new Dictionary<Category, decimal>(snapshot.CategoryLimits);
        trip.Budget.Expenses.Clear();

        foreach (var expense in snapshot.Expenses)
        {
            var participant = trip.Participants.FirstOrDefault(p => p.Id == expense.Participant.Id);
            if (participant is not null)
            {
                trip.Budget.Expenses.Add(new ExpenseEntry(expense.Amount, expense.Category, expense.Date, participant));
            }
        }
    }

    // Updates activities that still exist, re-adds deleted ones and removes ones
    // that did not exist in the snapshot. Same ids are kept so votes and schedules still match.
    private static void RestoreActivities(Trip trip, List<TripActivity> snapshot)
    {
        var snapshotIds = snapshot.Select(a => a.Id).ToHashSet();
        trip.Activities.RemoveAll(a => !snapshotIds.Contains(a.Id));

        foreach (var saved in snapshot)
        {
            var current = trip.Activities.FirstOrDefault(a => a.Id == saved.Id);
            if (current is null)
            {
                trip.Activities.Add(saved.Clone());
                continue;
            }

            current.Name = saved.Name;
            current.Category = saved.Category;
            current.Location = saved.Location;
            current.Duration = saved.Duration;
            current.EstimatedCost = saved.EstimatedCost;
            current.IsOptional = saved.IsOptional;
            current.OpeningTime = saved.OpeningTime;
            current.ClosingTime = saved.ClosingTime;
            current.Votes = new Dictionary<Guid, VoteType>(saved.Votes);
        }
    }

    private static Place ToPlace(TripActivity activity)
    {
        return new Place
        {
            Name = activity.Name,
            Location = activity.Location,
            VisitDuration = activity.Duration,
            OpeningTime = activity.OpeningTime ?? TimeSpan.Zero,
            ClosingTime = activity.ClosingTime ?? TimeSpan.Zero
        };
    }
}
