using Microsoft.AspNetCore.Mvc;
using TravelPlannerApp.Models;
using TravelPlannerApp.Budgeting;

namespace TravelPlannerApp.Controllers
{
    public class CreateTripRequest
    {
        public int Participants { get; set; }
        public Dictionary<string, decimal> CategoryLimits { get; set; }
    }

    public class AddExpenseRequest
    {
        public decimal Amount { get; set; }
        public string Category { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class BudgetApiController : ControllerBase
    {
        private static Budget _budget;

        [HttpPost("create")]
        public IActionResult CreateTrip([FromBody] CreateTripRequest request)
        {
            var budget = new Budget { NumberOfParticipants = request.Participants };

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

            _budget.Expenses.Add(new ExpenseEntry(request.Amount, category, DateTime.Now));
            return Ok(new { message = "Expense added" });
        }

        [HttpGet("summary")]
        public IActionResult GetSummary()
        {
            if (_budget == null)
                return BadRequest(new { error = "No trip created yet" });

            var results = BudgetService.CheckAllCategories(_budget);
            var costPerPerson = BudgetService.GetCostPerPerson(_budget);
            return Ok(new { results, costPerPerson });
        }

        [HttpGet("checkall")]
        public IActionResult CheckAll()
        {
            var budget = new Budget
            {
                CategoryLimits = new() { { Category.Food, 200m }, { Category.Transport, 100m } },
                NumberOfParticipants = 3
            };
            budget.Expenses.Add(new ExpenseEntry(190m, Category.Food, DateTime.Now));

            var results = BudgetService.CheckAllCategories(budget);
            return Ok(results);
        }
    }
}