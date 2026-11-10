using TravelPlanner.Extensions;
using TravelPlanner.Models;
using TravelPlanner.Responses;

namespace TravelPlanner.Service;

public class SettlementService
{
    private readonly BudgetService _budgetService;
    private readonly TripService _tripService;

    public SettlementService(
        BudgetService budgetService,
        TripService tripService)
    {
        _budgetService = budgetService;
        _tripService = tripService;
    }

    public async Task<List<SettlementResponse>?> GetSettlementAsync(string tripCode)
    {
        var trip = await _tripService.FindTripForBudgetByCodeAsync(tripCode);

        if (trip is null)
        {
            return null;
        }

        var balances = CalculateBalances(trip.Budget, trip.Participants);
        var settlements = SimplifyDebts(balances);

        return settlements.Select(settlement => new SettlementResponse(
            GetParticipantName(trip, settlement.FromParticipantId),
            GetParticipantName(trip, settlement.ToParticipantId),
            settlement.Amount
        )).ToList();
    }

    public Dictionary<Guid, decimal> CalculateBalances(Budget budget, IEnumerable<Participant> participants)
    {
        var participantList = participants.ToList();
        var balances = new Dictionary<Guid, decimal>();

        if (participantList.Count == 0)
        {
            return balances;
        }

        var totalSpendings = _budgetService.GetTotalActualSpend(budget);
        decimal[] shares = totalSpendings.SplitEvenly(participantList.Count);

        for (int i = 0; i < participantList.Count; i++)
        {
            var participant = participantList[i];

            decimal paid = budget.Expenses
                .Where(e => e.Participant.Id == participant.Id)
                .Sum(e => e.Amount);

            balances[participant.Id] = paid - shares[i];
        }

        return balances;
    }

    public List<Settlement> SimplifyDebts(Dictionary<Guid, decimal> balances)
    {
        var settlements = new List<Settlement>();

        var debtors = balances
            .Where(b => b.Value < 0)
            .ToDictionary(b => b.Key, b => -b.Value);

        var creditors = balances
            .Where(b => b.Value > 0)
            .ToDictionary(b => b.Key, b => b.Value);

        while (debtors.Count > 0 && creditors.Count > 0)
        {
            var debtor = debtors.OrderByDescending(d => d.Value).First();
            var creditor = creditors.OrderByDescending(d => d.Value).First();

            var amount = Math.Min(debtor.Value, creditor.Value);

            settlements.Add(new Settlement(debtor.Key, creditor.Key, amount));

            debtors[debtor.Key] -= amount;
            creditors[creditor.Key] -= amount;

            if (debtors[debtor.Key] == 0)
            {
                debtors.Remove(debtor.Key);
            }

            if (creditors[creditor.Key] == 0)
            {
                creditors.Remove(creditor.Key);
            }
        }

        return settlements;
    }

    private static string GetParticipantName(Trip trip, Guid participantId)
    {
        var participant = trip.Participants.FirstOrDefault(candidate => candidate.Id == participantId);
        return participant?.Name ?? "Unknown participant";
    }
}