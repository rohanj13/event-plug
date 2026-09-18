using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Domain.Entities;

public class BudgetCategory
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public CategoryType CategoryType { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string Currency { get; set; } = string.Empty;

    public Event Event { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
