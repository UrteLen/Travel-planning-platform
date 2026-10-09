using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Service;
using TravelPlanner.Requests;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly BudgetService _budgetService;
        private readonly SettlementService _settlementService;
        private readonly SummaryService _summaryService;
        private readonly PdfExportService _pdfExportService;
        public BudgetController(TripService tripService, BudgetService budgetService,
                                SettlementService settlementService, SummaryService summaryService,
                                PdfExportService pdfExportService)
        {
            this._tripService = tripService;
            this._budgetService = budgetService;
            this._settlementService = settlementService;
            this._summaryService = summaryService;
            this._pdfExportService = pdfExportService;
        }

        [HttpPost("limits")]
        public IActionResult SetLimits([FromBody] SetBudgetLimitsRequest request)
        {
            var trip = _tripService.FindTripByCode(request.TripCode);

            if (trip == null)
                return NotFound(new { error = "Trip not found" });

            foreach (var pair in request.CategoryLimits)
            {
                if (Enum.TryParse<Category>(pair.Key, true, out var category))
                {
                    trip.Budget.CategoryLimits[category] = pair.Value;
                }
            }

            return Ok(new { message = "Budget limits set" });
        }

        [HttpPost("expense")]
        public IActionResult AddExpense([FromBody] AddExpenseRequest request)
        {
            if (!Enum.TryParse<Category>(request.Category, true, out var category))
                return BadRequest(new { error = "Invalid category" });

            var trip = _tripService.FindTripByCode(request.TripCode);

            if (trip == null)
                return BadRequest(new { error = "Trip not found" });

            if (!trip.Participants.TryGetValue(request.ParticipantId, out var participant))
                return BadRequest(new { error = "Participant not found" });

            trip.Budget.Expenses.Add(new ExpenseEntry(request.Amount, category, DateTime.Now, participant));
            return Ok(new { message = "Expense added" });
        }

        [HttpGet("summary")]
        public IActionResult GetSummary(string tripCode)
        {
            var trip = _tripService.FindTripByCode(tripCode);

            if (trip == null)
                return NotFound(new { error = "Trip not found" });

            var results = _budgetService.CheckAllCategories(trip.Budget);
            var costPerPerson = _budgetService.GetCostPerPerson(trip.Budget, trip.Participants.Count);
            return Ok(new { results, costPerPerson });
        }

        [HttpGet("settlement")]
        public IActionResult GetSettlement(string tripCode)
        {
            var trip = _tripService.FindTripByCode(tripCode);

            if (trip == null)
            {
                return NotFound(new { error = "Trip not found"});
            }

            var balances = _settlementService.CalculateBalances(trip.Budget, trip.Participants.Values);
            var settlements = _settlementService.SimplifyDebts(balances);

            var result = settlements.Select(s => new
            {
                from = GetParticipantName(trip, s.FromParticipantId),
                to = GetParticipantName(trip, s.ToParticipantId),
                amount = s.Amount
            }).ToList();

            return Ok(result);
        }

        private static string GetParticipantName(Trip trip, Guid participantId)
        {
            if (trip.Participants.TryGetValue(participantId, out var participant))
            {
                return participant.Name;
            }

            return "Unknown participant";
        }
        [HttpGet("export/{tripId}")]
        public IActionResult ExportSummary(Guid tripId)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip is null || trip.Budget is null)
            {
                return NotFound(new { error = "Trip or budget not found" });
            }

            int participantCount = trip.Participants.Count > 0 ? trip.Participants.Count : 1;
            var summary = _summaryService.GenerateSummary(trip.Budget, new List<PlannedVisit>(), participantCount);
            var pdfBytes = _pdfExportService.ExportToPdf(summary);

            return File(pdfBytes, "application/pdf", $"trip-summary-{tripId}.pdf");
        }
        // [HttpGet("checkall")]
        // public IActionResult CheckAll()
        // {
        //     var budget = new Budget
        //     {
        //         CategoryLimits = new() { { Category.Food, 200m }, { Category.Transport, 100m } },
        //         NumberOfParticipants = 3
        //     };
        //     budget.Expenses.Add(new ExpenseEntry(190m, Category.Food, DateTime.Now));

        //     var results = BudgetService.CheckAllCategories(budget);
        //     return Ok(results);
        // }

        // [HttpGet("create")]
        // public IActionResult Create()
        // {
        //     Console.WriteLine("Yay. Cia viskas veikia.");
        //     return Ok("As tikrai gyvas");
        // }

    }
}