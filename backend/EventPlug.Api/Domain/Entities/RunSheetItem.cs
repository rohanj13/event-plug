namespace EventPlug.Api.Domain.Entities;

public class RunSheetItem : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid EventId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OwnerUserId { get; set; }

    public Organization Organization { get; set; } = null!;
    public Event Event { get; set; } = null!;
    public User? OwnerUser { get; set; }
}
