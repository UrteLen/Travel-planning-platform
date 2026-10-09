using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Service;
using TravelPlanner.Requests;
using TravelPlanner.Models.Trips;
using TravelPlanner.Responses;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripController : ControllerBase
    {
        private readonly TripService _tripService;
        public TripController(TripService tripService)
        {
            this._tripService = tripService;
        }

        [HttpPost("create")]
        public IActionResult Create(CreateTripRequest request)
        {
            var trip = _tripService.CreateTrip(
                request.Name,
                request.Destination,
                request.StartDate,
                request.EndDate,
                request.OrganizerName);

            return Ok(ToTripResponse(trip));
        }

        [HttpPost("join")]
        public IActionResult Join(JoinTripRequest request)
        {
            var trip = _tripService.JoinTrip(request.InviteCode, request.ParticipantName);

            if (trip is null)
            {
                return NotFound("No trip found with that invite code.");
            }

            return Ok(ToTripResponse(trip));
        }

        private TripResponse ToTripResponse(Trip trip)
        {
            var participants = trip.Participants.ToDictionary(
                entry => entry.Key,
                entry => new ParticipantResponse(
                    entry.Value.Id,
                    entry.Value.Name,
                    entry.Value.Email,
                    entry.Value.Role));

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
                            expense.Participant.Email,
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
}