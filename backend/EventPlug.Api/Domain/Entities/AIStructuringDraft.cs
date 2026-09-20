using System.Text.Json;

namespace EventPlug.Api.Domain.Entities;

public class AIStructuringDraft
{
    public Guid Id { get; set; }
    public Guid? EventId { get; set; }
    public string RawInputText { get; set; } = string.Empty;
    public JsonDocument StructuredOutput { get; set; } = null!;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool Accepted { get; set; }

    public Event? Event { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
