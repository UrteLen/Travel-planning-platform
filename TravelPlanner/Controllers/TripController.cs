using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Service;
using TravelPlanner.Requests;

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

            return Ok(trip);
        }

        [HttpPost("join")]
        public IActionResult Join(JoinTripRequest request)
        {
            var trip = _tripService.JoinTrip(request.InviteCode, request.ParticipantName);

            if (trip is null)
            {
                return NotFound("No trip found with that invite code.");
            }

            return Ok(trip);
        }
    }
}