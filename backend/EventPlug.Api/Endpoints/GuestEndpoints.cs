using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class GuestEndpoints
{
    private const int MaxBulkRows = 1000;

    public static RouteGroupBuilder MapGuestEndpoints(this RouteGroupBuilder v1)
    {
        var guests = v1.MapGroup("/events/{eventId:guid}/guests").WithTags("Guests");

        guests.MapGet("", ListGuests)
            .WithName("listGuests")
            .WithSummary("Guests for an event, by name. Optional rsvpStatus filter.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        guests.MapGet("/summary", GetSummary)
            .WithName("getGuestSummary")
            .WithSummary("Guest counts per RSVP status.")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        guests.MapPost("", CreateGuest)
            .WithName("createGuest")
            .WithSummary("Add a guest. Planner on the owning team, or an admin. 409 if the email is already on the guest list.")
            .Produces<GuestResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        guests.MapPost("/bulk", BulkCreateGuests)
            .WithName("bulkCreateGuests")
            .WithSummary($"Add up to {MaxBulkRows} guests at once (e.g. from a directory upload). Valid rows are saved; duplicates are skipped and invalid rows are reported.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        guests.MapPatch("/{guestId:guid}", UpdateGuest)
            .WithName("updateGuest")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        guests.MapDelete("/{guestId:guid}", DeleteGuest)
            .WithName("deleteGuest")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        return v1;
    }

    private static async Task<ListResponse<GuestResponse>> ListGuests(
        Guid eventId,
        EventPlugDbContext db,
        RsvpStatus? rsvpStatus)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        var query = db.Guests.AsNoTracking().Where(g => g.EventId == eventId);
        if (rsvpStatus is not null)
        {
            query = query.Where(g => g.RsvpStatus == rsvpStatus);
        }

        var items = await query
            .OrderBy(g => g.Name).ThenBy(g => g.Email)
            .Select(g => ToResponse(g))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<GuestSummaryResponse> GetSummary(Guid eventId, EventPlugDbContext db)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        var counts = await db.Guests.AsNoTracking()
            .Where(g => g.EventId == eventId)
            .GroupBy(g => g.RsvpStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int Count(RsvpStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        return new GuestSummaryResponse(
            counts.Sum(c => c.Count),
            Count(RsvpStatus.Invited),
            Count(RsvpStatus.Yes),
            Count(RsvpStatus.No),
            Count(RsvpStatus.Maybe));
    }

    private static async Task<IResult> CreateGuest(
        Guid eventId,
        CreateGuestRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator();
        ValidateNew(request, validator);
        validator.ThrowIfInvalid();

        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var email = request.Email.Trim();
        await EnsureEmailIsFreeAsync(db, eventId, email, excludeGuestId: null);

        var guest = NewGuest(currentUser.OrganizationId, eventId, request, email);
        db.Guests.Add(guest);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/events/{eventId}/guests/{guest.Id}", ToResponse(guest));
    }

    private static async Task<BulkGuestsResponse> BulkCreateGuests(
        Guid eventId,
        BulkGuestsRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        new RequestValidator()
            .Check(request.Guests.Count > 0, "guests", "Provide at least one guest.")
            .Check(request.Guests.Count <= MaxBulkRows, "guests", $"Provide at most {MaxBulkRows} guests per request.")
            .ThrowIfInvalid();

        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var seen = (await db.Guests.AsNoTracking()
                .Where(g => g.EventId == eventId)
                .Select(g => g.Email)
                .ToListAsync())
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();

        var errors = new List<BulkGuestError>();
        var toAdd = new List<Guest>();
        var skipped = 0;

        for (var i = 0; i < request.Guests.Count; i++)
        {
            var row = request.Guests[i];
            var rowValidator = new RequestValidator();
            if (row is null)
            {
                errors.Add(new BulkGuestError(i + 1, "Row is empty."));
                continue;
            }

            ValidateNew(row, rowValidator);
            var rowErrors = rowValidator.Errors;
            if (rowErrors.Count > 0)
            {
                errors.Add(new BulkGuestError(i + 1, string.Join(" ", rowErrors.Select(e => $"{e.Key}: {string.Join(" ", e.Value)}"))));
                continue;
            }

            var email = row.Email.Trim();
            if (!seen.Add(email.ToLowerInvariant()))
            {
                skipped++;
                continue;
            }

            toAdd.Add(NewGuest(currentUser.OrganizationId, eventId, row, email));
        }

        if (toAdd.Count > 0)
        {
            db.Guests.AddRange(toAdd);
            await db.SaveChangesAsync();
        }

        return new BulkGuestsResponse(toAdd.Count, skipped, errors);
    }

    private static async Task<GuestResponse> UpdateGuest(
        Guid eventId,
        Guid guestId,
        UpdateGuestRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator()
            .NotBlankIfProvided(request.Name, "name", 200)
            .NotBlankIfProvided(request.Email, "email", 320)
            .MaxLength(request.DietaryNotes, "dietaryNotes", 2000)
            .MaxLength(request.AccessNotes, "accessNotes", 2000);
        if (request.Email is not null)
        {
            validator.Check(LooksLikeEmail(request.Email.Trim()), "email", "Must be a valid email address.");
        }

        validator.ThrowIfInvalid();

        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var guest = await db.Guests.SingleOrDefaultAsync(g => g.Id == guestId && g.EventId == eventId)
            ?? throw ApiException.NotFound("Guest");

        if (request.Email is not null)
        {
            var email = request.Email.Trim();
            await EnsureEmailIsFreeAsync(db, eventId, email, excludeGuestId: guest.Id);
            guest.Email = email;
        }

        if (request.Name is not null) guest.Name = request.Name.Trim();
        if (request.RsvpStatus is not null) guest.RsvpStatus = request.RsvpStatus.Value;
        if (request.DietaryNotes is not null) guest.DietaryNotes = Clean(request.DietaryNotes);
        if (request.AccessNotes is not null) guest.AccessNotes = Clean(request.AccessNotes);

        await db.SaveChangesAsync();
        return ToResponse(guest);
    }

    private static async Task<IResult> DeleteGuest(
        Guid eventId,
        Guid guestId,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var guest = await db.Guests.SingleOrDefaultAsync(g => g.Id == guestId && g.EventId == eventId)
            ?? throw ApiException.NotFound("Guest");

        db.Guests.Remove(guest);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static void ValidateNew(CreateGuestRequest request, RequestValidator validator)
    {
        validator
            .Required(request.Name, "name", 200)
            .Required(request.Email, "email", 320)
            .MaxLength(request.DietaryNotes, "dietaryNotes", 2000)
            .MaxLength(request.AccessNotes, "accessNotes", 2000);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            validator.Check(LooksLikeEmail(request.Email.Trim()), "email", "Must be a valid email address.");
        }
    }

    private static Guest NewGuest(Guid orgId, Guid eventId, CreateGuestRequest request, string email) => new()
    {
        OrgId = orgId,
        EventId = eventId,
        Name = request.Name.Trim(),
        Email = email,
        RsvpStatus = request.RsvpStatus ?? RsvpStatus.Invited,
        DietaryNotes = Clean(request.DietaryNotes),
        AccessNotes = Clean(request.AccessNotes)
    };

    private static async Task EnsureEmailIsFreeAsync(EventPlugDbContext db, Guid eventId, string email, Guid? excludeGuestId)
    {
        var lowered = email.ToLower();
        if (await db.Guests.AnyAsync(g => g.EventId == eventId && g.Email.ToLower() == lowered && g.Id != excludeGuestId))
        {
            throw ApiException.Conflict($"{email} is already on the guest list for this event.");
        }
    }

    internal static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 &&
               at == value.LastIndexOf('@') &&
               !value.Any(char.IsWhiteSpace) &&
               value[(at + 1)..].Contains('.') &&
               !value.EndsWith('.');
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static GuestResponse ToResponse(Guest g) =>
        new(g.Id, g.EventId, g.Name, g.Email, g.RsvpStatus, g.DietaryNotes, g.AccessNotes);
}
