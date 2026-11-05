using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class EventEndpoints
{
    public static RouteGroupBuilder MapEventEndpoints(this RouteGroupBuilder v1)
    {
        var events = v1.MapGroup("/events").WithTags("Events");

        events.MapGet("", ListEvents)
            .WithName("listEvents")
            .WithSummary("Events in the organisation, soonest first. 'from'/'to' return events overlapping that date range. teamId returns events that team is assigned to.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest);

        events.MapPost("", CreateEvent)
            .WithName("createEvent")
            .WithSummary("Create an event owned by one team. Caller must be a Planner on that team (or an admin).")
            .Produces<EventResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        events.MapGet("/{eventId:guid}", GetEvent)
            .WithName("getEvent")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        events.MapPatch("/{eventId:guid}", UpdateEvent)
            .WithName("updateEvent")
            .WithSummary("Update an event. Planner on the owning team, or an admin.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        events.MapDelete("/{eventId:guid}", DeleteEvent)
            .WithName("deleteEvent")
            .WithSummary("Delete an event with its teams, guests, run sheet and budget categories. 409 if vendors, AI drafts or expenses still reference it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        var teams = events.MapGroup("/{eventId:guid}/teams").WithTags("Event teams");

        teams.MapGet("", ListEventTeams)
            .WithName("listEventTeams")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        teams.MapPost("", AddEventTeam)
            .WithName("addEventTeam")
            .WithSummary("Assign another team to the event. Planner on the owning team, or an admin.")
            .Produces<EventTeamResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        teams.MapDelete("/{teamId:guid}", RemoveEventTeam)
            .WithName("removeEventTeam")
            .WithSummary("Unassign a team. The owning team cannot be removed; 409 if the team has expense splits on this event.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return v1;
    }

    private static async Task<ListResponse<EventResponse>> ListEvents(
        EventPlugDbContext db,
        DateOnly? from,
        DateOnly? to,
        EventStatus? status,
        Guid? teamId)
    {
        if (from is not null && to is not null && to < from)
        {
            throw ApiException.Validation(new Dictionary<string, string[]> { ["to"] = new[] { "Must not be before 'from'." } });
        }

        var query = db.Events.AsNoTracking();

        if (from is not null)
        {
            query = query.Where(e => (e.EndDate ?? e.EventDate) >= from);
        }

        if (to is not null)
        {
            query = query.Where(e => e.EventDate <= to);
        }

        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        if (teamId is not null)
        {
            query = query.Where(e => e.EventTeams.Any(et => et.TeamId == teamId));
        }

        var items = await Project(query.OrderBy(e => e.EventDate).ThenBy(e => e.Title)).ToListAsync();
        return ListResponse.From(items);
    }

    private static async Task<IResult> CreateEvent(
        CreateEventRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator()
            .Required(request.Title, "title", 250)
            .Required(request.Location, "location", 250)
            .MaxLength(request.BudgetBand, "budgetBand", 120)
            .Check(request.EventDate != default, "eventDate", "This field is required.")
            .Check(request.EndDate is null || request.EndDate >= request.EventDate, "endDate", "Must not be before eventDate.")
            .Check(request.GuestCountEstimate is null or >= 0, "guestCountEstimate", "Must be zero or more.");
        validator.ThrowIfInvalid();

        if (!await db.Teams.AnyAsync(t => t.Id == request.OwningTeamId))
        {
            throw ApiException.NotFound("Team");
        }

        await currentUser.RequireTeamRoleAsync(request.OwningTeamId, TeamRole.Planner);

        var orgId = currentUser.OrganizationId;
        var userId = currentUser.UserId;

        var evt = new Event
        {
            OrgId = orgId,
            Title = request.Title.Trim(),
            EventDate = request.EventDate,
            EndDate = request.EndDate,
            Location = request.Location.Trim(),
            GuestCountEstimate = request.GuestCountEstimate,
            BudgetBand = Clean(request.BudgetBand),
            StyleNotes = Clean(request.StyleNotes),
            Status = request.Status ?? EventStatus.Draft,
            IsShared = request.IsShared ?? false,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Approvals are not exposed yet; the column is required, so the creator stands in as approver.
        evt.EventTeams.Add(new EventTeam
        {
            OrgId = orgId,
            TeamId = request.OwningTeamId,
            IsOwningTeam = true,
            SplitMethod = SplitMethod.Even,
            ApproverUserId = userId
        });

        db.Events.Add(evt);
        await db.SaveChangesAsync();

        var response = await LoadAsync(db, evt.Id);
        return Results.Created($"/api/v1/events/{evt.Id}", response);
    }

    private static async Task<EventResponse> GetEvent(Guid eventId, EventPlugDbContext db) =>
        await LoadAsync(db, eventId);

    private static async Task<EventResponse> UpdateEvent(
        Guid eventId,
        UpdateEventRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        new RequestValidator()
            .NotBlankIfProvided(request.Title, "title", 250)
            .NotBlankIfProvided(request.Location, "location", 250)
            .MaxLength(request.BudgetBand, "budgetBand", 120)
            .Check(request.GuestCountEstimate is null or >= 0, "guestCountEstimate", "Must be zero or more.")
            .ThrowIfInvalid();

        var evt = await RequirePlannerAsync(db, currentUser, eventId);

        var eventDate = request.EventDate ?? evt.EventDate;
        var endDate = request.EndDate ?? evt.EndDate;
        new RequestValidator()
            .Check(endDate is null || endDate >= eventDate, "endDate", "Must not be before eventDate.")
            .ThrowIfInvalid();

        evt.EventDate = eventDate;
        evt.EndDate = endDate;
        if (request.Title is not null) evt.Title = request.Title.Trim();
        if (request.Location is not null) evt.Location = request.Location.Trim();
        if (request.GuestCountEstimate is not null) evt.GuestCountEstimate = request.GuestCountEstimate;
        if (request.BudgetBand is not null) evt.BudgetBand = Clean(request.BudgetBand);
        if (request.StyleNotes is not null) evt.StyleNotes = Clean(request.StyleNotes);
        if (request.Status is not null) evt.Status = request.Status.Value;
        if (request.IsShared is not null) evt.IsShared = request.IsShared.Value;

        await db.SaveChangesAsync();
        return await LoadAsync(db, eventId);
    }

    private static async Task<IResult> DeleteEvent(Guid eventId, CurrentUser currentUser, EventPlugDbContext db)
    {
        var evt = await RequirePlannerAsync(db, currentUser, eventId);

        db.Events.Remove(evt);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<ListResponse<EventTeamResponse>> ListEventTeams(Guid eventId, EventPlugDbContext db)
    {
        await EnsureEventExistsAsync(db, eventId);

        var items = await db.EventTeams.AsNoTracking()
            .Where(et => et.EventId == eventId)
            .OrderByDescending(et => et.IsOwningTeam).ThenBy(et => et.Team.Name)
            .Select(et => new EventTeamResponse(et.TeamId, et.Team.Name, et.IsOwningTeam))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<IResult> AddEventTeam(
        Guid eventId,
        AddEventTeamRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var evt = await RequirePlannerAsync(db, currentUser, eventId);

        var team = await db.Teams.AsNoTracking().SingleOrDefaultAsync(t => t.Id == request.TeamId)
            ?? throw ApiException.NotFound("Team");

        if (await db.EventTeams.AnyAsync(et => et.EventId == eventId && et.TeamId == team.Id))
        {
            throw ApiException.Conflict("This team is already assigned to the event.");
        }

        db.EventTeams.Add(new EventTeam
        {
            OrgId = currentUser.OrganizationId,
            EventId = evt.Id,
            TeamId = team.Id,
            IsOwningTeam = false,
            SplitMethod = SplitMethod.Even,
            ApproverUserId = evt.CreatedBy
        });
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/events/{eventId}/teams", new EventTeamResponse(team.Id, team.Name, false));
    }

    private static async Task<IResult> RemoveEventTeam(
        Guid eventId,
        Guid teamId,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await RequirePlannerAsync(db, currentUser, eventId);

        var link = await db.EventTeams.SingleOrDefaultAsync(et => et.EventId == eventId && et.TeamId == teamId)
            ?? throw ApiException.NotFound("Event team");

        if (link.IsOwningTeam)
        {
            throw ApiException.Conflict("The owning team cannot be removed from its event.");
        }

        var hasSplits = await db.ExpenseTeamSplits.AnyAsync(s =>
            s.TeamId == teamId && s.Expense.BudgetCategory.EventId == eventId);

        if (hasSplits)
        {
            throw ApiException.Conflict("This team has expense splits on this event. Remove the splits first.");
        }

        db.EventTeams.Remove(link);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static IQueryable<EventResponse> Project(IQueryable<Event> query) =>
        query.Select(e => new EventResponse(
            e.Id,
            e.Title,
            e.EventDate,
            e.EndDate,
            e.Location,
            e.Status,
            e.GuestCountEstimate,
            e.BudgetBand,
            e.StyleNotes,
            e.IsShared,
            e.EventTeams.Where(et => et.IsOwningTeam).Select(et => et.TeamId).FirstOrDefault(),
            e.EventTeams
                .OrderByDescending(et => et.IsOwningTeam).ThenBy(et => et.Team.Name)
                .Select(et => new EventTeamResponse(et.TeamId, et.Team.Name, et.IsOwningTeam))
                .ToList(),
            e.CreatedBy,
            e.CreatedAt));

    private static async Task<EventResponse> LoadAsync(EventPlugDbContext db, Guid eventId) =>
        await Project(db.Events.AsNoTracking().Where(e => e.Id == eventId)).SingleOrDefaultAsync()
            ?? throw ApiException.NotFound("Event");

    internal static async Task EnsureEventExistsAsync(EventPlugDbContext db, Guid eventId)
    {
        if (!await db.Events.AnyAsync(e => e.Id == eventId))
        {
            throw ApiException.NotFound("Event");
        }
    }

    /// <summary>Loads the tracked event and requires an admin or a Planner on its owning team.</summary>
    internal static async Task<Event> RequirePlannerAsync(EventPlugDbContext db, CurrentUser currentUser, Guid eventId)
    {
        var evt = await db.Events.SingleOrDefaultAsync(e => e.Id == eventId)
            ?? throw ApiException.NotFound("Event");

        var owningTeamId = await db.EventTeams
            .Where(et => et.EventId == eventId && et.IsOwningTeam)
            .Select(et => et.TeamId)
            .SingleAsync();

        await currentUser.RequireTeamRoleAsync(owningTeamId, TeamRole.Planner);
        return evt;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
