namespace EventPlug.Api.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<TeamMembership> Memberships { get; set; } = new List<TeamMembership>();
    public ICollection<EventTeam> EventTeams { get; set; } = new List<EventTeam>();
    public ICollection<ExpenseTeamSplit> ExpenseTeamSplits { get; set; } = new List<ExpenseTeamSplit>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
