using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Enums;
using TravelPlanner.Models;
using TravelPlanner.Requests;
using TravelPlanner.Responses;
using TravelPlanner.Service;

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

    [HttpPost("create")]
    public async Task<IActionResult> CreateAsync(CreateTripRequest request)
    {
        var (trip, validationError) = await _tripService.CreateTripAsync(
            request.Name,
            request.Destination,
            request.StartDate,
            request.EndDate,
            request.OrganizerName);

        if (trip is null)
        {
            return BadRequest(new { error = validationError });
        }

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
        var trip = await _tripService.JoinTripAsync(request.InviteCode, request.ParticipantName, role: UserRole.Participant);

        if (trip is null)
        {
            return NotFound("No trip found with that invite code.");
        }

        return Ok(ToTripResponse(trip));
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
