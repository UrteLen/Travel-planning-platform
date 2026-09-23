using System.Collections.Generic;
using System.Linq;
using TravelPlannerApp.Extensions;
using TravelPlannerApp.Models;

namespace TravelPlannerApp.Budgeting
{
    public static class BudgetService
    {
        public static decimal GetTotalSpent(Budget budget, Category category) 
        { 
            return budget.Expenses
                .Where(e => e.Category == category)
                .Sum(e => e.Amount);
        }

        public static CategoryBudgetResult CheckCategory(Budget budget, Category category) 
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

        public static List<CategoryBudgetResult> CheckAllCategories(Budget budget) 
        { 
             return budget.CategoryLimits.Keys
                    .Select(category => CheckCategory(budget, category))
                    .ToList();
        }

        public static decimal GetTotalActualSpend(Budget budget) 
        { 
            return budget.Expenses
                .Sum(e => e.Amount);
        }

        public static decimal GetTotalPlannedBudget(Budget budget) 
        { 
            return budget.CategoryLimits.Values.Sum();
        }

        public static decimal GetCostPerPerson(Budget budget) 
        { 
            return GetTotalActualSpend(budget).ToShare(budget.NumberOfParticipants);
        }
    }
} 