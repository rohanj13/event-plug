namespace EventPlug.Api.Domain.Entities;

public class ExpenseTeamSplit
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public Guid TeamId { get; set; }
    public decimal PortionAmount { get; set; }

    public Expense Expense { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
