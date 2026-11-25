using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class SummaryServiceTests
{
    private readonly SummaryService _service = new(new BudgetService(null!, null!), null!, null!);

    [Fact]
    public void GenerateSummary_CalculatesTotalsAndBreakdown()
    {
        var budget = new Budget();
        budget.CategoryLimits[Category.Food] = 100m;
        budget.CategoryLimits[Category.Transport] = 50m;
        budget.Expenses.Add(new ExpenseEntry(30m, Category.Food, DateTime.UtcNow, new Participant { Name = "A" }));
        var plan = new List<PlannedVisit>();

        var summary = _service.GenerateSummary(budget, plan, 3);

        Assert.Equal(150m, summary.TotalPlannedBudget);
        Assert.Equal(30m, summary.TotalActualSpend);
        Assert.Equal(10m, summary.CostPerPerson);
        Assert.Equal(2, summary.CategoryBreakdown.Count);
        Assert.Same(plan, summary.Itinerary);
    }

    [Fact]
    public void GenerateSummary_WithoutParticipantCount_DefaultsToOnePerson()
    {
        var budget = new Budget();
        budget.Expenses.Add(new ExpenseEntry(40m, Category.Food, DateTime.UtcNow, new Participant { Name = "A" }));

        var summary = _service.GenerateSummary(budget, new List<PlannedVisit>());

        Assert.Equal(40m, summary.CostPerPerson);
    }

    [Fact]
    public void GenerateSummary_SetsGeneratedAtToNow()
    {
        var summary = _service.GenerateSummary(new Budget(), new List<PlannedVisit>());

        Assert.InRange(summary.GeneratedAt, DateTime.Now.AddMinutes(-1), DateTime.Now.AddMinutes(1));
    }
}
