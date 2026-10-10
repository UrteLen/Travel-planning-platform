using TravelPlanner.Models.Geography;

namespace TravelPlanner.Models.Planning
{
    public class Place
    {
        public required string Name { get; set; }
        public GeoLocation Location { get; set; }
        public TimeSpan VisitDuration { get; set; }
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }

        public Place Clone () => new()
        {
            Name = Name,
            Location = Location,
            VisitDuration = VisitDuration,
            OpeningTime = OpeningTime,
            ClosingTime = ClosingTime
        };
    }
}