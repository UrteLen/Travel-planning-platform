using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Models;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripApiController : ControllerBase
    {
        public record CreateTripRequest(string Name, string Destination, DateTime StartDate, DateTime EndDate, string OrganizerName);
        public record JoinTripRequest(string InviteCode, string ParticipantName);

        [HttpPost("create")]
        public IActionResult Create(CreateTripRequest request)
        {
            var trip = TripService.CreateTrip(
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
            var trip = TripService.JoinTrip(request.InviteCode, request.ParticipantName);

            if (trip is null)
            {
                return NotFound("No trip found with that invite code.");
            }

            return Ok(trip);
        }
    }
}