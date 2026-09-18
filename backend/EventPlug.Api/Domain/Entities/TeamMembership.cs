using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class TeamMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TeamId { get; set; }
    public TeamRole TeamRole { get; set; }

    public User User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
