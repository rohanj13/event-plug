using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Data;

public class EventPlugDbContext(DbContextOptions<EventPlugDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventTeam> EventTeams => Set<EventTeam>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseTeamSplit> ExpenseTeamSplits => Set<ExpenseTeamSplit>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ApprovalAuditEntry> ApprovalAuditEntries => Set<ApprovalAuditEntry>();
    public DbSet<RunSheetItem> RunSheetItems => Set<RunSheetItem>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorEventLink> VendorEventLinks => Set<VendorEventLink>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<AIStructuringDraft> AiStructuringDrafts => Set<AIStructuringDraft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventPlugDbContext).Assembly);
    }
}
