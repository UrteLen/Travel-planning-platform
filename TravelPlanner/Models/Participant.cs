using TravelPlanner.Enums;

namespace TravelPlanner.Models;

public class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Participant;

    public Participant Clone() => new()
    {
        Id = Id,
        Name = Name,
        Role = Role
    };
}