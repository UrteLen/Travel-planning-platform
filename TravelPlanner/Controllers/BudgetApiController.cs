using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    public class SetBudgetLimitsRequest
    {
        public string TripCode { get; set; } = string.Empty;
        public Dictionary<string, decimal> CategoryLimits { get; set; } = new();
    }

    public class AddExpenseRequest
    {
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public Guid ParticipantId { get; set; }
        public string TripCode { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BudgetApiController : ControllerBase
    {

        [HttpPost("limits")]
        public IActionResult SetLimits([FromBody] SetBudgetLimitsRequest request)
        {
            var trip = TripService.FindTripByCode(request.TripCode);

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

            var trip = TripService.FindTripByCode(request.TripCode);

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
            var trip = TripService.FindTripByCode(tripCode);

            if (trip == null)
                return NotFound(new { error = "Trip not found" });

            var results = BudgetService.CheckAllCategories(trip.Budget);
            var costPerPerson = BudgetService.GetCostPerPerson(trip.Budget, trip.Participants.Count);
            return Ok(new { results, costPerPerson });
        }

        [HttpGet("settlement")]
        public IActionResult GetSettlement(string tripCode)
        {
            var trip = TripService.FindTripByCode(tripCode);

            if (trip == null)
            {
                return NotFound(new { error = "Trip not found"});
            }

            var balances = SettlementService.CalculateBalances(trip.Budget, trip.Participants.Values);
            var settlements = SettlementService.SimplifyDebts(balances);

            var result = settlements.Select(s => new
            {
                from = trip.Participants[s.FromParticipantId].Name,
                to = trip.Participants[s.ToParticipantId].Name,
                amount = s.Amount
            }).ToList();

            return Ok(result);
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
