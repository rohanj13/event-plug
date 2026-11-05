using System.Text.Json.Serialization;
using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Contracts;

public sealed record BudgetCategoryResponse(
    Guid Id,
    Guid EventId,
    CategoryType CategoryType,
    decimal AllocatedAmount,
    string Currency,
    decimal SpentAmount);

/// <summary>Currency is a 3-letter ISO code and defaults to AUD. One category per type per event.</summary>
public sealed record CreateBudgetCategoryRequest(
    [property: JsonRequired] CategoryType CategoryType,
    decimal AllocatedAmount,
    string? Currency);

/// <summary>Omitted (null) fields are left unchanged.</summary>
public sealed record UpdateBudgetCategoryRequest(
    decimal? AllocatedAmount,
    string? Currency);

public sealed record ExpenseResponse(
    Guid Id,
    Guid EventId,
    Guid BudgetCategoryId,
    CategoryType CategoryType,
    string Description,
    decimal Amount,
    string Currency,
    Guid? VendorId,
    string? VendorName,
    ExpenseStatus Status,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record CreateExpenseRequest(
    string Description,
    decimal Amount,
    Guid? VendorId);

/// <summary>Omitted (null) fields are left unchanged. budgetCategoryId must belong to the same event.</summary>
public sealed record UpdateExpenseRequest(
    string? Description,
    decimal? Amount,
    Guid? VendorId,
    Guid? BudgetCategoryId);

public sealed record CategorySummary(
    Guid BudgetCategoryId,
    CategoryType CategoryType,
    decimal Allocated,
    decimal Spent,
    decimal Remaining);

public sealed record TeamShare(Guid TeamId, string TeamName, decimal Amount);

/// <summary>Calculated on request. Spent excludes Rejected expenses. Team shares come from expense splits.</summary>
public sealed record BudgetSummaryResponse(
    Guid EventId,
    string? Currency,
    decimal TotalAllocated,
    decimal TotalSpent,
    decimal Remaining,
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<TeamShare> TeamShares);
