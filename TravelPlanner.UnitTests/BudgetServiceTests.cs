using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class BudgetServiceTests
{
    private readonly BudgetService _service = new(null!, null!);

    private static Budget CreateBudget(decimal foodLimit, params decimal[] foodExpenses)
    {
        var budget = new Budget();
        budget.CategoryLimits[Category.Food] = foodLimit;
        var participant = new Participant { Name = "A" };

        foreach (var amount in foodExpenses)
        {
            budget.Expenses.Add(new ExpenseEntry(amount, Category.Food, DateTime.UtcNow, participant));
        }

        return budget;
    }

    [Fact]
    public void GetTotalSpent_CountsOnlyTheRequestedCategory()
    {
        var budget = CreateBudget(100m, 20m, 30m);
        budget.Expenses.Add(new ExpenseEntry(500m, Category.Transport, DateTime.UtcNow, new Participant { Name = "B" }));

        Assert.Equal(50m, _service.GetTotalSpent(budget, Category.Food));
        Assert.Equal(500m, _service.GetTotalSpent(budget, Category.Transport));
        Assert.Equal(0m, _service.GetTotalSpent(budget, Category.Accommodation));
    }

    [Theory]
    [InlineData(50, BudgetStatus.Ok)]
    [InlineData(89.99, BudgetStatus.Ok)]
    [InlineData(90, BudgetStatus.Warning)]
    [InlineData(100, BudgetStatus.Warning)]
    [InlineData(100.01, BudgetStatus.Exceeded)]
    public void CheckCategory_ReturnsStatusByHowMuchOfTheLimitIsSpent(double spent, BudgetStatus expected)
    {
        var budget = CreateBudget(100m, (decimal)spent);

        var result = _service.CheckCategory(budget, Category.Food);

        Assert.Equal(expected, result.Status);
        Assert.Equal(100m, result.Limit);
        Assert.Equal((decimal)spent, result.ActualSpent);
        Assert.Equal(Category.Food, result.Category);
    }

    [Fact]
    public void CheckCategory_WhenCategoryHasNoLimitAndMoneyWasSpent_IsExceeded()
    {
        var budget = new Budget();
        budget.Expenses.Add(new ExpenseEntry(10m, Category.Transport, DateTime.UtcNow, new Participant { Name = "A" }));

        var result = _service.CheckCategory(budget, Category.Transport);

        Assert.Equal(0m, result.Limit);
        Assert.Equal(BudgetStatus.Exceeded, result.Status);
    }

    [Fact]
    public void CheckAllCategories_ReturnsOneResultPerCategoryWithALimit()
    {
        var budget = CreateBudget(100m, 10m);
        budget.CategoryLimits[Category.Transport] = 50m;

        var results = _service.CheckAllCategories(budget);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Category == Category.Food);
        Assert.Contains(results, r => r.Category == Category.Transport);
    }

    [Fact]
    public void CheckAllCategories_WithoutLimits_ReturnsEmptyList()
    {
        Assert.Empty(_service.CheckAllCategories(new Budget()));
    }

    [Fact]
    public void GetTotalPlannedBudget_SumsAllLimits()
    {
        var budget = CreateBudget(100m);
        budget.CategoryLimits[Category.Transport] = 50m;

        Assert.Equal(150m, _service.GetTotalPlannedBudget(budget));
    }

    [Fact]
    public void GetTotalActualSpend_SumsAllExpensesOfAllCategories()
    {
        var budget = CreateBudget(100m, 20m, 30m);
        budget.Expenses.Add(new ExpenseEntry(5m, Category.Transport, DateTime.UtcNow, new Participant { Name = "B" }));

        Assert.Equal(55m, _service.GetTotalActualSpend(budget));
    }

    [Fact]
    public void GetCostPerPerson_RoundsToTwoDecimals()
    {
        var budget = CreateBudget(100m, 100m);

        Assert.Equal(33.33m, _service.GetCostPerPerson(budget, 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AddExpenseAsync_WhenAmountIsNotPositive_ReturnsError(double amount)
    {
        var error = await _service.AddExpenseAsync("CODE", (decimal)amount, "Food", Guid.NewGuid());

        Assert.Equal("Amount must be greater than 0", error);
    }

    [Fact]
    public async Task AddExpenseAsync_WhenCategoryIsUnknown_ReturnsError()
    {
        var error = await _service.AddExpenseAsync("CODE", 10m, "Flying", Guid.NewGuid());

        Assert.Equal("Invalid category", error);
    }
}
