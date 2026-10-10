using TravelPlanner.Extensions;
using System.Linq;
using TravelPlanner.Models.Trips;
using TravelPlanner.Models.Budgets;

namespace TravelPlanner.Service
{
    public class SettlementService
    {
        private readonly BudgetService _budgetService;
        public SettlementService(BudgetService budgetService)
        {
            this._budgetService = budgetService;
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

                if(creditors[creditor.Key] == 0)
                {
                    creditors.Remove(creditor.Key);
                }
            }

            return settlements;
        }
    }

}