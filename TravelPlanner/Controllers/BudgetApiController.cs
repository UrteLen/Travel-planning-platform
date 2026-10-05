using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BudgetApiController : ControllerBase
    {
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

        [HttpGet("create")]
        public IActionResult Create()
        {
            Console.WriteLine("Yay. Cia viskas veikia.");
            return Ok("As tikrai gyvas");
        }

    }
}
