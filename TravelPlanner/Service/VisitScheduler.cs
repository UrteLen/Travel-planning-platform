using TravelPlanner.Models.Planning;

namespace TravelPlanner.Service
{
    public class VisitScheduler
    {
        public bool TrySchedule(DateTime arrival, Place place, out DateTime start, out DateTime end)
        {
            start = arrival;
            end = arrival + place.VisitDuration;

            return IsWithinOpeningHours(start, end, place.OpeningTime, place.ClosingTime);
        }

        private bool IsWithinOpeningHours(DateTime start, DateTime end, TimeSpan opening, TimeSpan closing)
        {
            DateTime openingDateTime;
            DateTime closingDateTime;

            if (closing > opening)
            {
                openingDateTime = start.Date + opening;
                closingDateTime = start.Date + closing;
            }
            else
            {
                if (start.TimeOfDay >= opening)
                {
                    openingDateTime = start.Date + opening;
                    closingDateTime = start.Date.AddDays(1) + closing;
                }
                else
                {
                    openingDateTime = start.Date.AddDays(-1) + opening;
                    closingDateTime = start.Date + closing;
                }
            }

            return start >= openingDateTime && end <= closingDateTime;
        }
    }
}