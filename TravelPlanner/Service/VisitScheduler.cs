using TravelPlanner.Models.Planning;

namespace TravelPlanner.Service
{
    public class VisitScheduler
    {
        public bool TrySchedule(DateTime arrival, Place place, out DateTime start, out DateTime end)
        {
            bool isOpen24_7 = place.OpeningTime == TimeSpan.Zero && place.ClosingTime == TimeSpan.Zero;

            if (isOpen24_7)
            {
                start = arrival;
                end = start + place.VisitDuration;
                return true;
            }

            var (openingDateTime, closingDateTime) = GetOpeningWindow(arrival, place.OpeningTime, place.ClosingTime);

            start = arrival < openingDateTime ? openingDateTime : arrival;
            end = start + place.VisitDuration;

            return start >= openingDateTime && end <= closingDateTime;
        }

        private (DateTime Opening, DateTime Closing) GetOpeningWindow(DateTime reference, TimeSpan opening, TimeSpan closing)
        {
            if (closing > opening)
            {
                return (reference.Date + opening, reference.Date + closing);
            }

            var todayOpening = reference.Date + opening;
            var todayClosing = reference.Date.AddDays(1) + closing;

            if (reference < todayClosing){

                if (reference.TimeOfDay < closing)
                {
                    return (reference.Date.AddDays(-1) + opening, reference.Date + closing);
                }
            }       
            return (todayOpening, todayClosing);
        }
    }
}