using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class ApprovalAuditEntry
{
    public Guid Id { get; set; }
    public Guid ApprovalId { get; set; }
    public Guid ActorId { get; set; }
    public ApprovalAction Action { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public Approval Approval { get; set; } = null!;
    public User Actor { get; set; } = null!;
}
