using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    public class CreateTripRequest
    {
        public int Participants { get; set; }
        public Dictionary<string, decimal> CategoryLimits { get; set; } = new();
    }

    public class AddExpenseRequest
    {
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = "Guest";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BudgetController : ControllerBase
    {
        private static Budget _budget = new();
        private static int _participantCount = 1;

        [HttpPost("create")]
        public IActionResult CreateTrip([FromBody] CreateTripRequest request)
        {
            var budget = new Budget();
            _participantCount = request.Participants > 0 ? request.Participants : 1;

            foreach (var pair in request.CategoryLimits)
            {
                if (Enum.TryParse<Category>(pair.Key, true, out var category))
                {
                    budget.CategoryLimits[category] = pair.Value;
                }
            }

            _budget = budget;
            return Ok(new { message = "Trip created" });
        }

        [HttpPost("expense")]
        public IActionResult AddExpense([FromBody] AddExpenseRequest request)
        {
            if (_budget == null)
                return BadRequest(new { error = "No trip created yet" });

            if (!Enum.TryParse<Category>(request.Category, true, out var category))
                return BadRequest(new { error = "Invalid category" });

            var participant = new Participant { Name = request.ParticipantName };
            _budget.Expenses.Add(new ExpenseEntry(request.Amount, category, DateTime.Now, participant));
            return Ok(new { message = "Expense added" });
        }

        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            if (_budget == null)
                return BadRequest(new { error = "No trip created yet" });

            var results = BudgetService.CheckAllCategories(_budget);
            var costPerPerson = BudgetService.GetCostPerPerson(_budget, _participantCount);
            return Ok(new { results, costPerPerson });
        }

        [HttpGet("checkall")]
        public IActionResult CheckAll()
        {
            var budget = new Budget
            {
                CategoryLimits = new() { { Category.Food, 200m }, { Category.Transport, 100m } }
            };
            var dummyParticipant = new Participant { Name = "Tester" };
            budget.Expenses.Add(new ExpenseEntry(190m, Category.Food, DateTime.Now, dummyParticipant));

            var results = BudgetService.CheckAllCategories(budget);
            return Ok(results);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return Ok("As tikrai gyvas");
        }

        [HttpGet("export")]
        public IActionResult ExportSummary()
        {
            if (_budget == null)
                return BadRequest(new { error = "No trip created yet" });

            var summary = SummaryService.GenerateSummary(_budget, new List<PlannedVisit>(), _participantCount);
            var pdfBytes = PdfExportService.ExportToPdf(summary);

            return File(pdfBytes, "application/pdf", "trip-summary.pdf");
        }
    }
}