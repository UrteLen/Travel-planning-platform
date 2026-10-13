using Microsoft.AspNetCore.Mvc;
using TravelPlanner.Requests;
using TravelPlanner.Service;

namespace TravelPlanner.Controllers
{
    [ApiController]
    [Route("api/trips/{tripId}/scheduled-activities")]
    public class ScheduleController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly ActivitySchedulingService _activitySchedulingService;

        public ScheduleController(TripService tripService, ActivitySchedulingService activitySchedulingService)
        {
            this._tripService = tripService;
            this._activitySchedulingService = activitySchedulingService;
        }

        [HttpPost]
        public IActionResult ScheduleActivity(
            Guid tripId,
            [FromBody] ScheduleActivityRequest request)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip == null)
            {
                return NotFound();
            }

            var activity = trip.Activities
                .FirstOrDefault(x => x.Id == request.ActivityId);
            if (activity == null)
            {
                return NotFound();
            }

            try
            {
                var scheduledActivity = _activitySchedulingService.ScheduleActivity(trip, activity,
                    request.StartTime, request.SpeedKmh);

                return Ok(scheduledActivity);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet]
        public IActionResult GetDailySchedule(
            Guid tripId,
            [FromQuery] DateTime date)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip == null)
            {
                return NotFound();
            }
            var dailySchedule = _activitySchedulingService.GetDailySchedule(trip.ScheduledActivities, date);
            return Ok(dailySchedule);
        }
        
        [HttpDelete("{scheduledActivityId}")]
        public IActionResult RemoveScheduledActivity(
            Guid tripId,
            Guid scheduledActivityId)
        {
            var trip = _tripService.FindTripById(tripId);
            if (trip == null)
            {
                return NotFound("Trip not found.");
            }

            if (!_activitySchedulingService.RemoveScheduledActivity(trip, scheduledActivityId)) 
            {
                return NotFound("Scheduled activity not found."); 
            }

            return NoContent();
        }
    }
}
