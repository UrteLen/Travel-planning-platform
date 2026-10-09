using System.Collections.Generic;
using System.Linq;
using TravelPlanner.Extensions;
using TravelPlanner.Models;

namespace TravelPlanner.Service
{
    public class BudgetService
    {

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

            }  else if (actual >= limit * 0.9m)
                {
                    status = BudgetStatus.Warning;

                } else
                  {
                    status = BudgetStatus.Ok;
                  }
            return  new CategoryBudgetResult(category, limit, actual, status);

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
}