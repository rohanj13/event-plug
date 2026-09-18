using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class EventTeam
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid TeamId { get; set; }
    public SplitMethod SplitMethod { get; set; }
    public decimal? SplitValue { get; set; }
    public bool IsOwningTeam { get; set; }
    public Guid ApproverUserId { get; set; }

    public Event Event { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public User ApproverUser { get; set; } = null!;
}
