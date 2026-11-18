using TravelPlanner.Enums;
using TravelPlanner.Extensions;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class SettlementTests
{
    private static SettlementService CreateService()
    {
        var budgetService = new BudgetService(null!, null!);
        return new SettlementService(budgetService, null!);
    }

    [Fact]
    public void SplitEvenly_WhenDivisible_GivesEqualShares()
    {
        var shares = 100m.SplitEvenly(4);

        Assert.Equal(new[] { 25m, 25m, 25m, 25m }, shares);
    }

    [Fact]
    public void SplitEvenly_WhenCentsRemain_GivesExtraCentsToFirstPeople()
    {
        var shares = 100m.SplitEvenly(3);

        Assert.Equal(new[] { 33.34m, 33.33m, 33.33m }, shares);
    }

    [Fact]
    public void SplitEvenly_WhenTwoCentsRemain_GivesOneCentToTwoPeople()
    {
        var shares = 100.01m.SplitEvenly(3);

        Assert.Equal(new[] { 33.34m, 33.34m, 33.33m }, shares);
    }

    [Theory]
    [InlineData(100, 3)]
    [InlineData(0.01, 3)]
    [InlineData(99.99, 7)]
    [InlineData(10, 1)]
    public void SplitEvenly_SharesAlwaysSumToTotal(double total, int people)
    {
        var shares = ((decimal)total).SplitEvenly(people);

        Assert.Equal((decimal)total, shares.Sum());
    }

    [Fact]
    public void CalculateBalances_WhenOnePersonPaidEverything_OthersOweTheirShare()
    {
        var payer = new Participant { Name = "A" };
        var second = new Participant { Name = "B" };
        var third = new Participant { Name = "C" };
        var budget = new Budget();
        budget.Expenses.Add(new ExpenseEntry(90m, Category.Food, DateTime.UtcNow, payer));

        var balances = CreateService().CalculateBalances(budget, new[] { payer, second, third });

        Assert.Equal(60m, balances[payer.Id]);
        Assert.Equal(-30m, balances[second.Id]);
        Assert.Equal(-30m, balances[third.Id]);
    }

    [Fact]
    public void CalculateBalances_WhenCentsRemain_BalancesSumToZero()
    {
        var first = new Participant { Name = "A" };
        var second = new Participant { Name = "B" };
        var third = new Participant { Name = "C" };
        var budget = new Budget();
        budget.Expenses.Add(new ExpenseEntry(100m, Category.Food, DateTime.UtcNow, first));

        var balances = CreateService().CalculateBalances(budget, new[] { first, second, third });

        Assert.Equal(0m, balances.Values.Sum());
        Assert.Equal(66.66m, balances[first.Id]);
        Assert.Equal(-33.33m, balances[second.Id]);
        Assert.Equal(-33.33m, balances[third.Id]);
    }

    [Fact]
    public void CalculateBalances_WithoutParticipants_ReturnsEmpty()
    {
        var balances = CreateService().CalculateBalances(new Budget(), Array.Empty<Participant>());

        Assert.Empty(balances);
    }

    [Fact]
    public void SimplifyDebts_WhenOneDebtorAndOneCreditor_ReturnsSinglePayment()
    {
        var creditor = Guid.NewGuid();
        var debtor = Guid.NewGuid();
        var balances = new Dictionary<Guid, decimal> { [creditor] = 10m, [debtor] = -10m };

        var settlements = CreateService().SimplifyDebts(balances);

        var settlement = Assert.Single(settlements);
        Assert.Equal(debtor, settlement.FromParticipantId);
        Assert.Equal(creditor, settlement.ToParticipantId);
        Assert.Equal(10m, settlement.Amount);
    }

    [Fact]
    public void SimplifyDebts_WhenTwoDebtors_MakesTwoPaymentsToCreditor()
    {
        var creditor = Guid.NewGuid();
        var small = Guid.NewGuid();
        var large = Guid.NewGuid();
        var balances = new Dictionary<Guid, decimal>
        {
            [creditor] = 20m,
            [small] = -5m,
            [large] = -15m
        };

        var settlements = CreateService().SimplifyDebts(balances);

        Assert.Equal(2, settlements.Count);
        Assert.All(settlements, s => Assert.Equal(creditor, s.ToParticipantId));
        Assert.Equal(20m, settlements.Sum(s => s.Amount));
    }

    [Fact]
    public void SimplifyDebts_WhenEveryoneIsEven_ReturnsNoPayments()
    {
        var balances = new Dictionary<Guid, decimal>
        {
            [Guid.NewGuid()] = 0m,
            [Guid.NewGuid()] = 0m
        };

        Assert.Empty(CreateService().SimplifyDebts(balances));
    }

    [Fact]
    public void SimplifyDebts_AfterCentSplit_SettlesAllDebts()
    {
        var first = new Participant { Name = "A" };
        var second = new Participant { Name = "B" };
        var third = new Participant { Name = "C" };
        var budget = new Budget();
        budget.Expenses.Add(new ExpenseEntry(100m, Category.Food, DateTime.UtcNow, first));
        var service = CreateService();

        var balances = service.CalculateBalances(budget, new[] { first, second, third });
        var settlements = service.SimplifyDebts(balances);

        Assert.Equal(2, settlements.Count);
        Assert.All(settlements, s => Assert.Equal(first.Id, s.ToParticipantId));
        Assert.Equal(66.66m, settlements.Sum(s => s.Amount));
    }
}