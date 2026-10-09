namespace TravelPlanner.Models
{
    public class Place
    {
        public required string Name { get; set; }
        public GeoLocation Location { get; set; }
        public TimeSpan VisitDuration { get; set; }
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }
    }
}