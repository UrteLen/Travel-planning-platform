using System;
using System.Collections.Generic;
using TravelPlanner.Models;

namespace TravelPlanner.Service
{
    public class SummaryService
    {
        private readonly BudgetService _budgetService;
        public SummaryService(BudgetService budgetService)
        {
            this._budgetService = budgetService;
        }

        public TripSummary GenerateSummary(Budget budget, List<PlannedVisit> itinerary, int numberOfParticipants = 1)
        {
            decimal totalPlanned = _budgetService.GetTotalPlannedBudget(budget);
            decimal totalActual = _budgetService.GetTotalActualSpend(budget);
            decimal costPerPerson = _budgetService.GetCostPerPerson(budget, numberOfParticipants);
            List<CategoryBudgetResult> breakdown = _budgetService.CheckAllCategories(budget);

            return new TripSummary(totalPlanned, totalActual, costPerPerson, breakdown, itinerary, DateTime.Now);
        }
    }
}