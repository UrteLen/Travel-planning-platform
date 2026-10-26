using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Service;
using TravelPlanner.Requests;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Responses;

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
    public BudgetController(TripService tripService, BudgetService budgetService,
                            SettlementService settlementService, SummaryService summaryService,
                            PdfExportService pdfExportService, TripVersionService versionService)
    {
        _tripService = tripService;
        _budgetService = budgetService;
        _settlementService = settlementService;
        _summaryService = summaryService;
        _pdfExportService = pdfExportService;
        _versionService = versionService;
    }

    [HttpPost("limits")]
    public IActionResult SetLimits([FromBody] SetBudgetLimitsRequest request)
    {
        var trip = _tripService.FindTripByCodeForUpdate(request.TripCode);
        if (trip == null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        var parsed = new Dictionary<Category, decimal>();
        foreach (var pair in request.CategoryLimits)
        {
            if (!Enum.TryParse<Category>(pair.Key, true, out var category) || !Enum.IsDefined(category))
            {
                return BadRequest(new { error = $"Unknown category: {pair.Key}" });
            }

            if (pair.Value < 0)
            {
                return BadRequest(new { error = $"Limit for {pair.Key} cannot be negative" });
            }

            parsed[category] = pair.Value;
        }

        foreach (var pair in parsed)
        {
            trip.Budget.CategoryLimits[pair.Key] = pair.Value;
        }

        _versionService.SaveVersion(trip.Id, trip.Budget, trip.Plan, trip.Activities, "limits updated");
        _tripService.SaveChanges();
        return Ok(new { message = "Budget limits set" });
    }

    [HttpPost("expense")]
    public IActionResult AddExpense([FromBody] AddExpenseRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than 0" });
        }

        if (!Enum.TryParse<Category>(request.Category, true, out var category))
        {
            return BadRequest(new { error = "Invalid category" });
        }

        var trip = _tripService.FindTripByCodeForUpdate(request.TripCode);

        if (trip == null)
        {
            return BadRequest(new { error = "Trip not found" });
        }

        var participant = trip.Participants.FirstOrDefault(p => p.Id == request.ParticipantId);
        if (participant is null)
        {
            return BadRequest(new { error = "Participant not found" });
        }

        trip.Budget.Expenses.Add(new ExpenseEntry(request.Amount, category, DateTime.UtcNow, participant));
        _versionService.SaveVersion(trip.Id, trip.Budget, trip.Plan, trip.Activities, participant.Name);
        _tripService.SaveChanges();
        return Ok(new { message = "Expense added" });
    }

    [HttpGet("summary")]
    public IActionResult GetSummary(string tripCode)
    {
        var trip = _tripService.FindTripByCode(tripCode);

        if (trip == null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        var results = _budgetService.CheckAllCategories(trip.Budget);
        var costPerPerson = _budgetService.GetCostPerPerson(trip.Budget, trip.Participants.Count);
        var totalSpent = _budgetService.GetTotalActualSpend(trip.Budget);
        return Ok(new BudgetSummaryResponse(results, costPerPerson, totalSpent));
    }

    [HttpGet("settlement")]
    public IActionResult GetSettlement(string tripCode)
    {
        var trip = _tripService.FindTripByCode(tripCode);

        if (trip == null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        var balances = _settlementService.CalculateBalances(trip.Budget, trip.Participants);
        var settlements = _settlementService.SimplifyDebts(balances);

        var result = settlements.Select(s => new SettlementResponse(
            GetParticipantName(trip, s.FromParticipantId),
            GetParticipantName(trip, s.ToParticipantId),
            s.Amount
        )).ToList();

        return Ok(result);
    }

    private static string GetParticipantName(Trip trip, Guid participantId)
    {
        var participant = trip.Participants.FirstOrDefault(p => p.Id == participantId);
        return participant?.Name ?? "Unknown participant";
    }
    [HttpGet("export/{tripId}")]
    public IActionResult ExportSummary(Guid tripId)
    {
        var trip = _tripService.FindTripById(tripId);
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