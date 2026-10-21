using System.Text.Json;
using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class QuoteRequest : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public Guid VendorEventLinkId { get; set; }
    public JsonDocument BriefSnapshot { get; set; } = null!;
    public string SentToEmail { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public QuoteRequestStatus Status { get; set; }

    public Organization Organization { get; set; } = null!;
    public VendorEventLink VendorEventLink { get; set; } = null!;
}
