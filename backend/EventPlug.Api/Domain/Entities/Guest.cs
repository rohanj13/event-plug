using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class Guest : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RsvpStatus RsvpStatus { get; set; }
    public string? DietaryNotes { get; set; }
    public string? AccessNotes { get; set; }

    public Organization Organization { get; set; } = null!;
    public Event Event { get; set; } = null!;
}
