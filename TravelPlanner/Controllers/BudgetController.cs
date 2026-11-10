using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
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

    public BudgetController(
        TripService tripService,
        BudgetService budgetService,
        SettlementService settlementService,
        SummaryService summaryService,
        PdfExportService pdfExportService)
    {
        _tripService = tripService;
        _budgetService = budgetService;
        _settlementService = settlementService;
        _summaryService = summaryService;
        _pdfExportService = pdfExportService;
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
        var error = await _budgetService.AddExpenseAsync(
            request.TripCode,
            request.Amount,
            request.Category,
            request.ParticipantId);

        if (error is not null)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Expense added" });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryAsync(string tripCode)
    {
        var summary = await _budgetService.GetSummaryAsync(tripCode);

        if (summary is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        return Ok(summary);
    }

    [HttpGet("settlement")]
    public async Task<IActionResult> GetSettlementAsync(string tripCode)
    {
        var settlements = await _settlementService.GetSettlementAsync(tripCode);

        if (settlements is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        return Ok(settlements);
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
