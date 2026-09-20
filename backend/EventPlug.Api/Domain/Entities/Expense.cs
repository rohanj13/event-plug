using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; }
    public Guid BudgetCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? VendorId { get; set; }
    public ExpenseStatus Status { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public BudgetCategory BudgetCategory { get; set; } = null!;
    public Vendor? Vendor { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ExpenseTeamSplit> ExpenseTeamSplits { get; set; } = new List<ExpenseTeamSplit>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}
