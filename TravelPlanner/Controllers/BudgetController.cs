using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BudgetController : ControllerBase
{
    private readonly TripService _tripService;
    private readonly BudgetService _budgetService;
    private readonly SettlementService _settlementService;
    private readonly SummaryService _summaryService;
    private readonly PdfExportService _pdfExportService;
    private readonly TripVersionService _versionService;

    public BudgetController(
        TripService tripService,
        BudgetService budgetService,
        SettlementService settlementService,
        SummaryService summaryService,
        PdfExportService pdfExportService,
        TripVersionService versionService)
    {
        _tripService = tripService;
        _budgetService = budgetService;
        _settlementService = settlementService;
        _summaryService = summaryService;
        _pdfExportService = pdfExportService;
        _versionService = versionService;
    }

    [HttpPost("limits")]
    public async Task<IActionResult> SetLimitsAsync(
        [FromBody] SetBudgetLimitsRequest request)
    {
        var result = await _budgetService.SetLimitsAsync(
            request.TripCode,
            request.CategoryLimits);

        if (!result.TripFound)
        {
            return NotFound(new { error = "Trip not found" });
        }

        if (result.ValidationError is not null)
        {
            return BadRequest(new { error = result.ValidationError });
        }

        return Ok(new { message = "Budget limits set" });
    }

    [HttpPost("expense")]
    public async Task<IActionResult> AddExpenseAsync(
        [FromBody] AddExpenseRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than 0" });
        }

        if (!Enum.TryParse<Category>(request.Category, true, out var category))
        {
            return BadRequest(new { error = "Invalid category" });
        }

        var trip = await _tripService.FindTripByCodeForUpdateAsync(request.TripCode);

        if (trip is null)
        {
            return BadRequest(new { error = "Trip not found" });
        }

        try
        {
            var participant = trip.Participants.FirstOrDefault(candidate => candidate.Id == request.ParticipantId);
            if (participant is null)
            {
                return BadRequest(new { error = "Participant not found" });
            }

            trip.Budget.Expenses.Add(new ExpenseEntry(request.Amount, category, DateTime.UtcNow, participant));
            await _versionService.SaveVersionAsync(trip.Id, trip.Budget, trip.Plan, trip.Activities, participant.Name);
            await _tripService.SaveChangesAsync();
            return Ok(new { message = "Expense added" });
        }
        finally
        {
            await _tripService.RollbackTransactionAsync();
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryAsync(string tripCode)
    {
        var trip = await _tripService.FindTripByCodeAsync(tripCode);

        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        var results = _budgetService.CheckAllCategories(trip.Budget);
        var costPerPerson = _budgetService.GetCostPerPerson(trip.Budget, trip.Participants.Count);
        var totalSpent = _budgetService.GetTotalActualSpend(trip.Budget);
        return Ok(new BudgetSummaryResponse(results, costPerPerson, totalSpent));
    }

    [HttpGet("settlement")]
    public async Task<IActionResult> GetSettlementAsync(string tripCode)
    {
        var trip = await _tripService.FindTripByCodeAsync(tripCode);

        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        var balances = _settlementService.CalculateBalances(trip.Budget, trip.Participants);
        var settlements = _settlementService.SimplifyDebts(balances);

        var result = settlements.Select(settlement => new SettlementResponse(
            GetParticipantName(trip, settlement.FromParticipantId),
            GetParticipantName(trip, settlement.ToParticipantId),
            settlement.Amount
        )).ToList();

        return Ok(result);
    }

    private static string GetParticipantName(Trip trip, Guid participantId)
    {
        var participant = trip.Participants.FirstOrDefault(candidate => candidate.Id == participantId);
        return participant?.Name ?? "Unknown participant";
    }

    [HttpGet("export/{tripId}")]
    public async Task<IActionResult> ExportSummaryAsync(Guid tripId)
    {
        var trip = await _tripService.FindTripByIdAsync(tripId);
        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        int participantCount = trip.Participants.Count > 0 ? trip.Participants.Count : 1;
        var summary = _summaryService.GenerateSummary(trip.Budget, trip.Plan, numberOfParticipants: participantCount);
        var pdfBytes = _pdfExportService.ExportToPdf(summary);

        return File(pdfBytes, "application/pdf", $"trip-summary-{tripId}.pdf");
    }
}
