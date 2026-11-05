using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class RunSheetEndpoints
{
    public static RouteGroupBuilder MapRunSheetEndpoints(this RouteGroupBuilder v1)
    {
        var items = v1.MapGroup("/events/{eventId:guid}/run-sheet").WithTags("Run sheet");

        items.MapGet("", ListItems)
            .WithName("listRunSheetItems")
            .WithSummary("Run sheet items ordered by day, sortOrder, then start time. Optional day filter.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        items.MapPost("", CreateItem)
            .WithName("createRunSheetItem")
            .WithSummary("Add an item. Planner on the owning team, or an admin.")
            .Produces<RunSheetItemResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        items.MapPatch("/{itemId:guid}", UpdateItem)
            .WithName("updateRunSheetItem")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        items.MapDelete("/{itemId:guid}", DeleteItem)
            .WithName("deleteRunSheetItem")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        return v1;
    }

    private static async Task<ListResponse<RunSheetItemResponse>> ListItems(
        Guid eventId,
        EventPlugDbContext db,
        int? day)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        if (day is < 1)
        {
            throw ApiException.Validation(new Dictionary<string, string[]> { ["day"] = new[] { "Must be 1 or more." } });
        }

        var query = db.RunSheetItems.AsNoTracking().Where(i => i.EventId == eventId);
        if (day is not null)
        {
            query = query.Where(i => i.Day == day);
        }

        var items = await query
            .OrderBy(i => i.Day).ThenBy(i => i.SortOrder).ThenBy(i => i.StartTime)
            .Select(i => new RunSheetItemResponse(
                i.Id, i.EventId, i.Day, i.StartTime, i.EndTime, i.Title, i.Description,
                i.OwnerUserId, i.OwnerUser == null ? null : i.OwnerUser.Name, i.SortOrder))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<IResult> CreateItem(
        Guid eventId,
        CreateRunSheetItemRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator()
            .Required(request.Title, "title", 250)
            .MaxLength(request.Description, "description", 4000)
            .Check(request.StartTime != default, "startTime", "This field is required.")
            .Check(request.EndTime is null || request.EndTime >= request.StartTime, "endTime", "Must not be before startTime.")
            .Check(request.Day is null or >= 1, "day", "Must be 1 or more.")
            .Check(request.SortOrder is null or >= 0, "sortOrder", "Must be zero or more.");
        validator.ThrowIfInvalid();

        var evt = await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);
        var day = request.Day ?? 1;
        EnsureDayWithinEvent(evt, day);
        var ownerName = await ResolveOwnerNameAsync(db, request.OwnerUserId);

        var sortOrder = request.SortOrder
            ?? await db.RunSheetItems.Where(i => i.EventId == eventId && i.Day == day)
                .Select(i => (int?)i.SortOrder).MaxAsync() + 1
            ?? 0;

        var item = new RunSheetItem
        {
            OrgId = currentUser.OrganizationId,
            EventId = eventId,
            Title = request.Title.Trim(),
            Description = Clean(request.Description),
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime?.ToUniversalTime(),
            Day = day,
            SortOrder = sortOrder,
            OwnerUserId = request.OwnerUserId
        };

        db.RunSheetItems.Add(item);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/events/{eventId}/run-sheet/{item.Id}", ToResponse(item, ownerName));
    }

    private static async Task<RunSheetItemResponse> UpdateItem(
        Guid eventId,
        Guid itemId,
        UpdateRunSheetItemRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        new RequestValidator()
            .NotBlankIfProvided(request.Title, "title", 250)
            .MaxLength(request.Description, "description", 4000)
            .Check(request.Day is null or >= 1, "day", "Must be 1 or more.")
            .Check(request.SortOrder is null or >= 0, "sortOrder", "Must be zero or more.")
            .ThrowIfInvalid();

        var evt = await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var item = await db.RunSheetItems.SingleOrDefaultAsync(i => i.Id == itemId && i.EventId == eventId)
            ?? throw ApiException.NotFound("Run sheet item");

        var startTime = request.StartTime?.ToUniversalTime() ?? item.StartTime;
        var endTime = request.EndTime?.ToUniversalTime() ?? item.EndTime;
        new RequestValidator()
            .Check(endTime is null || endTime >= startTime, "endTime", "Must not be before startTime.")
            .ThrowIfInvalid();

        if (request.Day is not null)
        {
            EnsureDayWithinEvent(evt, request.Day.Value);
            item.Day = request.Day.Value;
        }

        if (request.OwnerUserId is not null)
        {
            await ResolveOwnerNameAsync(db, request.OwnerUserId);
            item.OwnerUserId = request.OwnerUserId;
        }

        item.StartTime = startTime;
        item.EndTime = endTime;
        if (request.Title is not null) item.Title = request.Title.Trim();
        if (request.Description is not null) item.Description = Clean(request.Description);
        if (request.SortOrder is not null) item.SortOrder = request.SortOrder.Value;

        await db.SaveChangesAsync();

        var ownerName = item.OwnerUserId is null ? null : await ResolveOwnerNameAsync(db, item.OwnerUserId);
        return ToResponse(item, ownerName);
    }

    private static async Task<IResult> DeleteItem(
        Guid eventId,
        Guid itemId,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var item = await db.RunSheetItems.SingleOrDefaultAsync(i => i.Id == itemId && i.EventId == eventId)
            ?? throw ApiException.NotFound("Run sheet item");

        db.RunSheetItems.Remove(item);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static void EnsureDayWithinEvent(Event evt, int day)
    {
        var days = evt.EndDate is null ? 1 : evt.EndDate.Value.DayNumber - evt.EventDate.DayNumber + 1;
        if (day > days)
        {
            throw ApiException.Validation(new Dictionary<string, string[]>
            {
                ["day"] = new[] { $"This event runs for {days} day{(days == 1 ? string.Empty : "s")}." }
            });
        }
    }

    private static async Task<string?> ResolveOwnerNameAsync(EventPlugDbContext db, Guid? ownerUserId)
    {
        if (ownerUserId is null)
        {
            return null;
        }

        var name = await db.Users.AsNoTracking()
            .Where(u => u.Id == ownerUserId)
            .Select(u => u.Name)
            .SingleOrDefaultAsync();

        return name ?? throw ApiException.Validation(new Dictionary<string, string[]>
        {
            ["ownerUserId"] = new[] { "User was not found in this organisation." }
        });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RunSheetItemResponse ToResponse(RunSheetItem i, string? ownerName) =>
        new(i.Id, i.EventId, i.Day, i.StartTime, i.EndTime, i.Title, i.Description, i.OwnerUserId, ownerName, i.SortOrder);
}
