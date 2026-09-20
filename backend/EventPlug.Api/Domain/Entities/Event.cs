using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public Guid OrgId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public int? GuestCountEstimate { get; set; }
    public string? BudgetBand { get; set; }
    public string? StyleNotes { get; set; }
    public EventStatus Status { get; set; }
    public bool IsShared { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<EventTeam> EventTeams { get; set; } = new List<EventTeam>();
    public ICollection<Guest> Guests { get; set; } = new List<Guest>();
    public ICollection<BudgetCategory> BudgetCategories { get; set; } = new List<BudgetCategory>();
    public ICollection<RunSheetItem> RunSheetItems { get; set; } = new List<RunSheetItem>();
    public ICollection<VendorEventLink> VendorEventLinks { get; set; } = new List<VendorEventLink>();
    public ICollection<AIStructuringDraft> AiStructuringDrafts { get; set; } = new List<AIStructuringDraft>();
}
