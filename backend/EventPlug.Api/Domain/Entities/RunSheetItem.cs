namespace EventPlug.Api.Domain.Entities;

public class RunSheetItem
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? OwnerUserId { get; set; }

    public Event Event { get; set; } = null!;
    public User? OwnerUser { get; set; }
}
