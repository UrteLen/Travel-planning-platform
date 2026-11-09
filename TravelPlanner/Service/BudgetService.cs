using TravelPlanner.Extensions;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public class BudgetService
{
    private readonly TripService _tripService;
    private readonly TripVersionService _versionService;

    public BudgetService(
        TripService tripService,
        TripVersionService versionService)
    {
        _tripService = tripService;
        _versionService = versionService;
    }

    public async Task<(bool TripFound, string? ValidationError)> SetLimitsAsync(
        string tripCode,
        Dictionary<string, decimal> categoryLimits)
    {
        var trip = await _tripService.FindTripByCodeForUpdateAsync(tripCode);

        if (trip is null)
        {
            return (false, null);
        }

        try
        {
            var parsed = new Dictionary<Category, decimal>();
            foreach (var pair in categoryLimits)
            {
                if (!Enum.TryParse<Category>(pair.Key, true, out var category) || !Enum.IsDefined(category))
                {
                    return (true, $"Unknown category: {pair.Key}");
                }

                if (pair.Value < 0)
                {
                    return (true, $"Limit for {pair.Key} cannot be negative");
                }

                parsed[category] = pair.Value;
            }

            foreach (var pair in parsed)
            {
                trip.Budget.CategoryLimits[pair.Key] = pair.Value;
            }

            await _versionService.SaveVersionAsync(trip.Id, trip.Budget, trip.Plan, trip.Activities, "limits updated");
            await _tripService.SaveChangesAsync();
            return (true, null);
        }
        finally
        {
            await _tripService.RollbackTransactionAsync();
        }
    }

    public decimal GetTotalSpent(Budget budget, Category category)
    {
        return budget.Expenses
            .Where(e => e.Category == category)
            .Sum(e => e.Amount);
    }

    public CategoryBudgetResult CheckCategory(Budget budget, Category category)
    {
        decimal actual = GetTotalSpent(budget, category);
        decimal limit = budget.CategoryLimits.GetValueOrDefault(category, 0m);

        BudgetStatus status;
        if (actual > limit)
        {
            status = BudgetStatus.Exceeded;
        }
        else if (actual >= limit * 0.9m)
        {
            status = BudgetStatus.Warning;
        }
        else
        {
            status = BudgetStatus.Ok;
        }

        return new CategoryBudgetResult(category, limit, actual, status);
    }

    public List<CategoryBudgetResult> CheckAllCategories(Budget budget)
    {
        return budget.CategoryLimits.Keys
            .Select(category => CheckCategory(budget, category))
            .ToList();
    }

    public decimal GetTotalActualSpend(Budget budget)
    {
        return budget.Expenses
            .Sum(e => e.Amount);
    }

    public decimal GetTotalPlannedBudget(Budget budget)
    {
        return budget.CategoryLimits.Values.Sum();
    }

    public decimal GetCostPerPerson(Budget budget, int numberOfParticipants)
    {
        return GetTotalActualSpend(budget).ToShare(numberOfParticipants);
    }
}
