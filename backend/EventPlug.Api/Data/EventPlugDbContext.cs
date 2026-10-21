using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Data;

public class EventPlugDbContext : DbContext
{
    private readonly ITenantContext tenantContext;

    public EventPlugDbContext(
        DbContextOptions<EventPlugDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        this.tenantContext = tenantContext;
    }

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

        ApplyTenantFilter<User>(modelBuilder);
        ApplyTenantFilter<Team>(modelBuilder);
        ApplyTenantFilter<TeamMembership>(modelBuilder);
        ApplyTenantFilter<Event>(modelBuilder);
        ApplyTenantFilter<EventTeam>(modelBuilder);
        ApplyTenantFilter<Guest>(modelBuilder);
        ApplyTenantFilter<BudgetCategory>(modelBuilder);
        ApplyTenantFilter<Expense>(modelBuilder);
        ApplyTenantFilter<ExpenseTeamSplit>(modelBuilder);
        ApplyTenantFilter<Approval>(modelBuilder);
        ApplyTenantFilter<ApprovalAuditEntry>(modelBuilder);
        ApplyTenantFilter<RunSheetItem>(modelBuilder);
        ApplyTenantFilter<Vendor>(modelBuilder);
        ApplyTenantFilter<VendorEventLink>(modelBuilder);
        ApplyTenantFilter<QuoteRequest>(modelBuilder);
        ApplyTenantFilter<AIStructuringDraft>(modelBuilder);
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => entity.OrgId == tenantContext.OrganizationId);
    }
}
