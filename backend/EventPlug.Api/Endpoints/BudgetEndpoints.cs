using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class BudgetEndpoints
{
    private const string DefaultCurrency = "AUD";

    public static RouteGroupBuilder MapBudgetEndpoints(this RouteGroupBuilder v1)
    {
        var cats = v1.MapGroup("/events/{eventId:guid}").WithTags("Budget");

        cats.MapGet("/budget-categories", ListCategories)
            .WithName("listBudgetCategories")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        cats.MapPost("/budget-categories", CreateCategory)
            .WithName("createBudgetCategory")
            .WithSummary("Planner on the owning team, or an admin. 409 if the event already has that category type.")
            .Produces<BudgetCategoryResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        cats.MapPatch("/budget-categories/{categoryId:guid}", UpdateCategory)
            .WithName("updateBudgetCategory")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        cats.MapDelete("/budget-categories/{categoryId:guid}", DeleteCategory)
            .WithName("deleteBudgetCategory")
            .WithSummary("409 while the category still has expenses.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        cats.MapGet("/budget-summary", GetSummary)
            .WithName("getBudgetSummary")
            .WithSummary("Allocated, spent and remaining per category, plus each team's share from expense splits.")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        cats.MapGet("/expenses", ListExpenses)
            .WithName("listExpenses")
            .WithSummary("Expenses for an event, newest first. Optional categoryId filter.")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        cats.MapPost("/budget-categories/{categoryId:guid}/expenses", CreateExpense)
            .WithName("createExpense")
            .WithSummary("Planner on the owning team, or an admin. Status starts as Pending.")
            .Produces<ExpenseResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        var expenses = v1.MapGroup("/expenses").WithTags("Budget");

        expenses.MapPatch("/{expenseId:guid}", UpdateExpense)
            .WithName("updateExpense")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        expenses.MapDelete("/{expenseId:guid}", DeleteExpense)
            .WithName("deleteExpense")
            .WithSummary("Splits are removed with it. 409 if approvals reference the expense.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return v1;
    }

    // ---- Categories ----

    private static async Task<ListResponse<BudgetCategoryResponse>> ListCategories(Guid eventId, EventPlugDbContext db)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        var items = await db.BudgetCategories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryType)
            .Select(c => new BudgetCategoryResponse(
                c.Id, c.EventId, c.CategoryType, c.AllocatedAmount, c.Currency,
                c.Expenses.Where(x => x.Status != ExpenseStatus.Rejected).Sum(x => (decimal?)x.Amount) ?? 0m))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<IResult> CreateCategory(
        Guid eventId,
        CreateBudgetCategoryRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator();
        ValidateMoney(validator, request.AllocatedAmount, "allocatedAmount", allowZero: true);
        ValidateCurrency(validator, request.Currency);
        validator.ThrowIfInvalid();

        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var type = request.CategoryType;
        if (await db.BudgetCategories.AnyAsync(c => c.EventId == eventId && c.CategoryType == type))
        {
            throw ApiException.Conflict($"This event already has a {type} budget category.");
        }

        var category = new BudgetCategory
        {
            OrgId = currentUser.OrganizationId,
            EventId = eventId,
            CategoryType = type,
            AllocatedAmount = request.AllocatedAmount,
            Currency = (request.Currency ?? DefaultCurrency).ToUpperInvariant()
        };

        db.BudgetCategories.Add(category);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/v1/events/{eventId}/budget-categories/{category.Id}",
            new BudgetCategoryResponse(category.Id, eventId, type, category.AllocatedAmount, category.Currency, 0m));
    }

    private static async Task<BudgetCategoryResponse> UpdateCategory(
        Guid eventId,
        Guid categoryId,
        UpdateBudgetCategoryRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator();
        if (request.AllocatedAmount is not null)
        {
            ValidateMoney(validator, request.AllocatedAmount.Value, "allocatedAmount", allowZero: true);
        }

        ValidateCurrency(validator, request.Currency);
        validator.ThrowIfInvalid();

        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var category = await db.BudgetCategories.SingleOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId)
            ?? throw ApiException.NotFound("Budget category");

        if (request.AllocatedAmount is not null) category.AllocatedAmount = request.AllocatedAmount.Value;
        if (request.Currency is not null) category.Currency = request.Currency.ToUpperInvariant();
        await db.SaveChangesAsync();

        var spent = await db.Expenses
            .Where(x => x.BudgetCategoryId == categoryId && x.Status != ExpenseStatus.Rejected)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;

        return new BudgetCategoryResponse(category.Id, eventId, category.CategoryType, category.AllocatedAmount, category.Currency, spent);
    }

    private static async Task<IResult> DeleteCategory(
        Guid eventId,
        Guid categoryId,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var category = await db.BudgetCategories.SingleOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId)
            ?? throw ApiException.NotFound("Budget category");

        if (await db.Expenses.AnyAsync(x => x.BudgetCategoryId == categoryId))
        {
            throw ApiException.Conflict("This category still has expenses. Delete or move them first.");
        }

        db.BudgetCategories.Remove(category);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<BudgetSummaryResponse> GetSummary(Guid eventId, EventPlugDbContext db)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        var categories = await db.BudgetCategories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryType)
            .Select(c => new
            {
                c.Id,
                c.CategoryType,
                c.AllocatedAmount,
                c.Currency,
                Spent = c.Expenses.Where(x => x.Status != ExpenseStatus.Rejected).Sum(x => (decimal?)x.Amount) ?? 0m
            })
            .ToListAsync();

        var shares = await db.ExpenseTeamSplits.AsNoTracking()
            .Where(s => s.Expense.BudgetCategory.EventId == eventId && s.Expense.Status != ExpenseStatus.Rejected)
            .GroupBy(s => new { s.TeamId, s.Team.Name })
            .Select(g => new TeamShare(g.Key.TeamId, g.Key.Name, g.Sum(s => s.PortionAmount)))
            .ToListAsync();

        var allocated = categories.Sum(c => c.AllocatedAmount);
        var spent = categories.Sum(c => c.Spent);

        return new BudgetSummaryResponse(
            eventId,
            categories.Select(c => c.Currency).Distinct().Count() == 1 ? categories[0].Currency : null,
            allocated,
            spent,
            allocated - spent,
            categories.Select(c => new CategorySummary(c.Id, c.CategoryType, c.AllocatedAmount, c.Spent, c.AllocatedAmount - c.Spent)).ToList(),
            shares.OrderBy(s => s.TeamName).ToList());
    }

    // ---- Expenses ----

    private static async Task<ListResponse<ExpenseResponse>> ListExpenses(Guid eventId, EventPlugDbContext db, Guid? categoryId)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        var query = db.Expenses.AsNoTracking().Where(x => x.BudgetCategory.EventId == eventId);
        if (categoryId is not null)
        {
            query = query.Where(x => x.BudgetCategoryId == categoryId);
        }

        var items = await Project(query.OrderByDescending(x => x.CreatedAt)).ToListAsync();
        return ListResponse.From(items);
    }

    private static async Task<IResult> CreateExpense(
        Guid eventId,
        Guid categoryId,
        CreateExpenseRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator().Required(request.Description, "description", 500);
        ValidateMoney(validator, request.Amount, "amount", allowZero: false);
        validator.ThrowIfInvalid();

        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var category = await db.BudgetCategories.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == categoryId && c.EventId == eventId)
            ?? throw ApiException.NotFound("Budget category");

        await EnsureVendorExistsAsync(db, request.VendorId);

        var expense = new Expense
        {
            OrgId = currentUser.OrganizationId,
            BudgetCategoryId = category.Id,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            VendorId = request.VendorId,
            Status = ExpenseStatus.Pending,
            CreatedBy = currentUser.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Expenses.Add(expense);
        await db.SaveChangesAsync();

        var response = await Project(db.Expenses.AsNoTracking().Where(x => x.Id == expense.Id)).SingleAsync();
        return Results.Created($"/api/v1/events/{eventId}/expenses?categoryId={categoryId}", response);
    }

    private static async Task<ExpenseResponse> UpdateExpense(
        Guid expenseId,
        UpdateExpenseRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator().NotBlankIfProvided(request.Description, "description", 500);
        if (request.Amount is not null)
        {
            ValidateMoney(validator, request.Amount.Value, "amount", allowZero: false);
        }

        validator.ThrowIfInvalid();

        var expense = await db.Expenses.Include(x => x.BudgetCategory).SingleOrDefaultAsync(x => x.Id == expenseId)
            ?? throw ApiException.NotFound("Expense");
        var eventId = expense.BudgetCategory.EventId;
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        if (request.BudgetCategoryId is not null && request.BudgetCategoryId != expense.BudgetCategoryId)
        {
            var targetId = request.BudgetCategoryId.Value;
            if (!await db.BudgetCategories.AnyAsync(c => c.Id == targetId && c.EventId == eventId))
            {
                throw ApiException.Validation(new Dictionary<string, string[]>
                {
                    ["budgetCategoryId"] = new[] { "Category was not found on this event." }
                });
            }

            expense.BudgetCategoryId = targetId;
        }

        if (request.VendorId is not null)
        {
            await EnsureVendorExistsAsync(db, request.VendorId);
            expense.VendorId = request.VendorId;
        }

        if (request.Description is not null) expense.Description = request.Description.Trim();
        if (request.Amount is not null) expense.Amount = request.Amount.Value;

        await db.SaveChangesAsync();

        return await Project(db.Expenses.AsNoTracking().Where(x => x.Id == expenseId)).SingleAsync();
    }

    private static async Task<IResult> DeleteExpense(Guid expenseId, CurrentUser currentUser, EventPlugDbContext db)
    {
        var expense = await db.Expenses.Include(x => x.BudgetCategory).SingleOrDefaultAsync(x => x.Id == expenseId)
            ?? throw ApiException.NotFound("Expense");
        await EventEndpoints.RequirePlannerAsync(db, currentUser, expense.BudgetCategory.EventId);

        if (await db.Approvals.AnyAsync(a => a.ExpenseId == expenseId))
        {
            throw ApiException.Conflict("This expense has approvals and cannot be deleted.");
        }

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // ---- Helpers ----

    private static IQueryable<ExpenseResponse> Project(IQueryable<Expense> query) =>
        query.Select(x => new ExpenseResponse(
            x.Id,
            x.BudgetCategory.EventId,
            x.BudgetCategoryId,
            x.BudgetCategory.CategoryType,
            x.Description,
            x.Amount,
            x.BudgetCategory.Currency,
            x.VendorId,
            x.Vendor == null ? null : x.Vendor.Name,
            x.Status,
            x.CreatedBy,
            x.CreatedAt));

    private static async Task EnsureVendorExistsAsync(EventPlugDbContext db, Guid? vendorId)
    {
        if (vendorId is not null && !await db.Vendors.AnyAsync(v => v.Id == vendorId))
        {
            throw ApiException.Validation(new Dictionary<string, string[]>
            {
                ["vendorId"] = new[] { "Vendor was not found in this organisation." }
            });
        }
    }

    private static void ValidateMoney(RequestValidator validator, decimal value, string field, bool allowZero)
    {
        validator
            .Check(allowZero ? value >= 0 : value > 0, field, allowZero ? "Must be zero or more." : "Must be greater than zero.")
            .Check(value == decimal.Round(value, 2), field, "Use at most 2 decimal places.")
            .Check(value < 10_000_000_000_000_000m, field, "Amount is too large.");
    }

    private static void ValidateCurrency(RequestValidator validator, string? currency)
    {
        validator.Check(
            currency is null || (currency.Length == 3 && currency.All(char.IsAsciiLetter)),
            "currency",
            "Must be a 3-letter currency code, e.g. AUD.");
    }
}
