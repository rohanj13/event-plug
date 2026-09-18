using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public OrgRole OrgRole { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<TeamMembership> TeamMemberships { get; set; } = new List<TeamMembership>();
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public ICollection<EventTeam> EventTeamApprovals { get; set; } = new List<EventTeam>();
    public ICollection<Expense> CreatedExpenses { get; set; } = new List<Expense>();
    public ICollection<Approval> RequestedApprovals { get; set; } = new List<Approval>();
    public ICollection<Approval> DecidingApprovals { get; set; } = new List<Approval>();
    public ICollection<ApprovalAuditEntry> ApprovalAuditEntries { get; set; } = new List<ApprovalAuditEntry>();
    public ICollection<RunSheetItem> RunSheetItems { get; set; } = new List<RunSheetItem>();
    public ICollection<AIStructuringDraft> AiStructuringDrafts { get; set; } = new List<AIStructuringDraft>();
}
