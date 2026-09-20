using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class Guest
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RsvpStatus RsvpStatus { get; set; }
    public string? DietaryNotes { get; set; }
    public string? AccessNotes { get; set; }

    public Event Event { get; set; } = null!;
}
