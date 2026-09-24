namespace TravelPlannerApp.Models
{
    public class Trip
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
        public List<Participant> Participants { get; set; } = new();
        public Budget? budget { get; set; }
        public string InviteCode { get; init; } = string.Empty;
    }
}