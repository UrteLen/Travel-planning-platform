using TravelPlanner.Models;
using TravelPlanner.Extensions;

namespace TravelPlanner.Service
{
    public static class SettlementService
    {
        public static Dictionary<Guid, decimal> CalculateBalances(Budget budget, IEnumerable<Participant> participants)
        {
            var participantList = participants.ToList();
            var balances = new Dictionary<Guid, decimal>();

            if (participantList.Count == 0)
            {
                return balances;
            }

            var totalSpendings = BudgetService.GetTotalActualSpend(budget);
            decimal[] shares = totalSpendings.SplitEvenly(participantList.Count);

            for (int i = 0; i < participantList.Count; i++)
            {
                var participant = participantList[i];

                decimal paid = 0;
                foreach (var e in budget.Expenses)
                {
                    if (e.Participant.Id == participant.Id)
                    {
                        paid += e.Amount;
                    }
                }

                balances[participant.Id] = paid - shares[i];
            }

            return balances;
        }
    }
    
}