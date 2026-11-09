using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;
using Microsoft.AspNetCore.Authorization;

namespace TravelPlanner.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripController : ControllerBase
{
    private readonly TripService _tripService;
    public TripController(TripService tripService)
    {
        _tripService = tripService;
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreateAsync(CreateTripRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Destination) || string.IsNullOrWhiteSpace(request.OrganizerName))
        {
            return BadRequest(new { error = "Name, destination and organizer name are required" });
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new { error = "End date cannot be earlier than start date" });
        }

        if (request.StartDate.Date < DateTime.Today)
        {
            return BadRequest(new { error = "Start date cannot be in the past" });
        }

        var trip = await _tripService.CreateTripAsync(
            request.Name,
            request.Destination,
            request.StartDate,
            request.EndDate,
            request.OrganizerName,
            GetUserId());

        return Ok(ToTripResponse(trip));
    }

    [HttpGet("{tripId:guid}")]
    public async Task<IActionResult> GetAsync(Guid tripId)
    {
        var trip = await _tripService.FindTripByIdAsync(tripId);
        if (trip is null)
        {
            return NotFound(new { error = "Trip not found" });
        }

        return Ok(ToTripResponse(trip));
    }


    [HttpPost("join")]
    public async Task<IActionResult> JoinAsync(JoinTripRequest request)
    {
        Guid? userId = null;
        if (request.LinkToAccount)
        {
            userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized();
            }
        }

        var trip = await _tripService.JoinTripAsync(
            request.InviteCode,
            request.ParticipantName,
            role: UserRole.Participant,
            userId: userId);
            
        if (trip is null)
        {
            return NotFound("No trip found with that invite code.");
        }

        return Ok(ToTripResponse(trip));
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> MineAsync()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var trips = await _tripService.FindTripsByUserAsync(userId.Value);
        return Ok(trips.Select(t => new
        {
            t.Id,
            t.Name,
            t.Destination,
            t.StartDate,
            t.EndDate,
            t.InviteCode,
            Role = t.Participants.First(p => p.UserId == userId).Role.ToString()
        }));
    }

    private Guid? GetUserId()
    {
        return Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    }
    private TripResponse ToTripResponse(Trip trip)
    {
        var participants = trip.Participants.ToDictionary(
            participant => participant.Id,
            participant => new ParticipantResponse(
                participant.Id,
                participant.Name,
                participant.Role));

        var activities = trip.Activities.Select(activity =>
                new ActivityResponse(
                    activity.Id,
                    activity.Name,
                    activity.Category,
                    new GeoLocationResponse(
                        activity.Location.Latitude,
                        activity.Location.Longitude),
                    activity.Duration,
                    activity.EstimatedCost,
                    activity.IsOptional,
                    activity.OpeningTime,
                    activity.ClosingTime))
            .ToList();

        var expenses = trip.Budget.Expenses.Select(expense =>
                new ExpenseResponse(
                    expense.Amount,
                    expense.Category,
                    expense.Date,
                    new ParticipantResponse(
                        expense.Participant.Id,
                        expense.Participant.Name,
                        expense.Participant.Role)))
            .ToList();

        var budget = new BudgetResponse(
            trip.Budget.CategoryLimits.ToDictionary(
                entry => entry.Key,
                entry => entry.Value),
            expenses);

        return new TripResponse(
            trip.Id,
            trip.Name,
            trip.Destination,
            trip.StartDate,
            trip.EndDate,
            participants,
            activities,
            budget,
            trip.InviteCode);
    }
}
