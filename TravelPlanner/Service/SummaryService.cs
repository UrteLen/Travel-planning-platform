using TravelPlanner.Models;
using TravelPlanner.Responses;
using TravelPlanner.Reports;

namespace TravelPlanner.Service;

public class SummaryService
{
    private readonly BudgetService _budgetService;
    private readonly TripService _tripService;
    private readonly PdfExportService _pdfExportService;

    public SummaryService(
        BudgetService budgetService,
        TripService tripService,
        PdfExportService pdfExportService)
    {
        _budgetService = budgetService;
        _tripService = tripService;
        _pdfExportService = pdfExportService;
    }

    public async Task<byte[]?> ExportSummaryAsync(Guid tripId)
    {
        var trip = await _tripService.FindTripForSummaryByIdAsync(tripId);

        if (trip is null)
        {
            return null;
        }

        int participantCount = Math.Max(1, trip.Participants.Count);
        var summary = GenerateSummary(trip.Budget, trip.Plan, participantCount);

        return _pdfExportService.ExportToPdf(summary);
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