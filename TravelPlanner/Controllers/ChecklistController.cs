using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers;

[ApiController]
[Authorize]
[Route("api/trips/{tripId:guid}/checklist")]
public class ChecklistController : ControllerBase
{
    private readonly ChecklistService _checklistService;

    public ChecklistController(ChecklistService checklistService)
    {
        _checklistService = checklistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(Guid tripId)
    {
        var checklist = await _checklistService.GetAsync(tripId, GetUserId());

        return Ok(checklist);
    }

    [HttpPost]
    public async Task<IActionResult> AddAsync(Guid tripId, AddChecklistItemRequest request)
    {
        try
        {
            var item = await _checklistService.AddItemAsync(
                tripId,
                GetUserId(),
                request.Text,
                request.Category);

            return Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{itemId:guid}/done")]
    public async Task<IActionResult> SetDoneAsync(
        Guid tripId,
        Guid itemId,
        SetChecklistItemDoneRequest request)
    {
        var item = await _checklistService.SetDoneAsync(tripId, GetUserId(), itemId, request.IsDone);

        return Ok(item);
    }

    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> RemoveAsync(Guid tripId, Guid itemId)
    {
        await _checklistService.RemoveItemAsync(tripId, GetUserId(), itemId);

        return NoContent();
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirst("sub")!.Value);
    }
}