using System;
using System.Collections.Generic;
using TravelPlanner.Models;

namespace TravelPlanner.Service
{
    public static class SummaryService
    {
        public static TripSummary GenerateSummary(Budget budget, List<PlannedVisit> itinerary, int numberOfParticipants = 1)
        {
            decimal totalPlanned = BudgetService.GetTotalPlannedBudget(budget);
            decimal totalActual = BudgetService.GetTotalActualSpend(budget);
            decimal costPerPerson = BudgetService.GetCostPerPerson(budget, numberOfParticipants);
            List<CategoryBudgetResult> breakdown = BudgetService.CheckAllCategories(budget);

            return new TripSummary(totalPlanned, totalActual, costPerPerson, breakdown, itinerary, DateTime.Now);
        }
    }
}