using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class VendorEventLink
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid VendorId { get; set; }
    public VendorEventStatus Status { get; set; }

    public Event Event { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
    public ICollection<QuoteRequest> QuoteRequests { get; set; } = new List<QuoteRequest>();
}
