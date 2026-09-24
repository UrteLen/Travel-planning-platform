namespace TravelPlannerApp.Models
{
    public enum UserRole
    {
        Organizer,
        Participant,
        ViewOnly
    }

    public class Participant
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Participant;
    }
}