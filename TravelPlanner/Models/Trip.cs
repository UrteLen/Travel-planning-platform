namespace TravelPlanner.Models
{
    public class Trip
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public Dictionary<Guid, Participant> Participants { get; set; } = new();
        public List<TripActivity> Activities { get; set; } = new();
        public Budget? Budget { get; set; } = new();
        public string InviteCode { get; init; } = string.Empty;
    }
}