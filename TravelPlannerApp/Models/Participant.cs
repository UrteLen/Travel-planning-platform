namespace TravelPlannerApp.Models
{
    public enum UserRole
    {
        Organizer,
        Participant
    }

    public class Participant
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Participant;

        public bool CanManageParticipants()
        {
            return Role == UserRole.Organizer;
        }

        public bool CanEditBudget()
        {
            return Role == UserRole.Organizer;
        }
    }

}