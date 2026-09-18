using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class Approval
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid TeamId { get; set; }
    public Guid RequestedBy { get; set; }
    public Guid ApproverId { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }

    public Expense Expense { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User ApproverUser { get; set; } = null!;
    public ICollection<ApprovalAuditEntry> AuditEntries { get; set; } = new List<ApprovalAuditEntry>();
}
