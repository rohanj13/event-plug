namespace EventPlug.Api.Domain.Entities;

public class Vendor
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<VendorEventLink> VendorEventLinks { get; set; } = new List<VendorEventLink>();
}
