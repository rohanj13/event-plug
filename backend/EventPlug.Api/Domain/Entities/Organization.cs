using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SubscriptionTier SubscriptionTier { get; set; }
    public DateOnly BillingCycleStart { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
