using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class VendorEndpoints
{
    public static RouteGroupBuilder MapVendorEndpoints(this RouteGroupBuilder v1)
    {
        var vendors = v1.MapGroup("/vendors").WithTags("Vendors");

        vendors.MapGet("", ListVendors)
            .WithName("listVendors")
            .WithSummary("The organisation's vendor directory, by name. Optional category filter and name search (q).");

        vendors.MapPost("", CreateVendor)
            .WithName("createVendor")
            .WithSummary("Admin, or a Planner on any team.")
            .Produces<VendorResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest);

        vendors.MapGet("/{vendorId:guid}", GetVendor)
            .WithName("getVendor")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        vendors.MapPatch("/{vendorId:guid}", UpdateVendor)
            .WithName("updateVendor")
            .WithSummary("Admin, or a Planner on any team.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        vendors.MapDelete("/{vendorId:guid}", DeleteVendor)
            .WithName("deleteVendor")
            .WithSummary("Admin only. 409 while the vendor is linked to an event or used by an expense.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        var links = v1.MapGroup("/events/{eventId:guid}/vendors").WithTags("Vendors");

        links.MapGet("", ListEventVendors)
            .WithName("listEventVendors")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        links.MapPost("", AddEventVendor)
            .WithName("addEventVendor")
            .WithSummary("Link a vendor to the event. Planner on the owning team, or an admin. 409 if already linked.")
            .Produces<EventVendorResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        links.MapPatch("/{vendorId:guid}", UpdateEventVendor)
            .WithName("updateEventVendor")
            .WithSummary("Change the vendor's status for this event.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        links.MapDelete("/{vendorId:guid}", RemoveEventVendor)
            .WithName("removeEventVendor")
            .WithSummary("Unlink a vendor. 409 if quote requests exist for this link.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return v1;
    }

    // ---- Vendor directory ----

    private static async Task<ListResponse<VendorResponse>> ListVendors(EventPlugDbContext db, string? category, string? q)
    {
        var query = db.Vendors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim().ToLower();
            query = query.Where(v => v.Category.ToLower() == c);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(v => EF.Functions.ILike(v.Name, pattern));
        }

        var items = await Project(query.OrderBy(v => v.Name)).ToListAsync();
        return ListResponse.From(items);
    }

    private static async Task<IResult> CreateVendor(CreateVendorRequest request, CurrentUser currentUser, EventPlugDbContext db)
    {
        var validator = new RequestValidator()
            .Required(request.Name, "name", 200)
            .Required(request.Category, "category", 100);
        ValidateContact(validator, request.ContactEmail, request.ContactPhone, request.Notes);
        validator.ThrowIfInvalid();

        await currentUser.RequirePlannerOnAnyTeamAsync();

        var vendor = new Vendor
        {
            OrgId = currentUser.OrganizationId,
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            ContactEmail = Clean(request.ContactEmail),
            ContactPhone = Clean(request.ContactPhone),
            Notes = Clean(request.Notes)
        };

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/vendors/{vendor.Id}", ToResponse(vendor));
    }

    private static async Task<VendorResponse> GetVendor(Guid vendorId, EventPlugDbContext db) =>
        await Project(db.Vendors.AsNoTracking().Where(v => v.Id == vendorId)).SingleOrDefaultAsync()
            ?? throw ApiException.NotFound("Vendor");

    private static async Task<VendorResponse> UpdateVendor(
        Guid vendorId,
        UpdateVendorRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        var validator = new RequestValidator()
            .NotBlankIfProvided(request.Name, "name", 200)
            .NotBlankIfProvided(request.Category, "category", 100);
        ValidateContact(validator, request.ContactEmail, request.ContactPhone, request.Notes);
        validator.ThrowIfInvalid();

        await currentUser.RequirePlannerOnAnyTeamAsync();

        var vendor = await db.Vendors.SingleOrDefaultAsync(v => v.Id == vendorId)
            ?? throw ApiException.NotFound("Vendor");

        if (request.Name is not null) vendor.Name = request.Name.Trim();
        if (request.Category is not null) vendor.Category = request.Category.Trim();
        if (request.ContactEmail is not null) vendor.ContactEmail = Clean(request.ContactEmail);
        if (request.ContactPhone is not null) vendor.ContactPhone = Clean(request.ContactPhone);
        if (request.Notes is not null) vendor.Notes = Clean(request.Notes);

        await db.SaveChangesAsync();
        return ToResponse(vendor);
    }

    private static async Task<IResult> DeleteVendor(Guid vendorId, CurrentUser currentUser, EventPlugDbContext db)
    {
        currentUser.RequireAdmin();

        var vendor = await db.Vendors.SingleOrDefaultAsync(v => v.Id == vendorId)
            ?? throw ApiException.NotFound("Vendor");

        if (await db.VendorEventLinks.AnyAsync(l => l.VendorId == vendorId))
        {
            throw ApiException.Conflict("This vendor is linked to one or more events. Unlink it first.");
        }

        if (await db.Expenses.AnyAsync(x => x.VendorId == vendorId))
        {
            throw ApiException.Conflict("This vendor is used by one or more expenses.");
        }

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // ---- Event links ----

    private static async Task<ListResponse<EventVendorResponse>> ListEventVendors(Guid eventId, EventPlugDbContext db)
    {
        await EventEndpoints.EnsureEventExistsAsync(db, eventId);

        var items = await ProjectLinks(db.VendorEventLinks.AsNoTracking()
                .Where(l => l.EventId == eventId)
                .OrderBy(l => l.Vendor.Name))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<IResult> AddEventVendor(
        Guid eventId,
        AddEventVendorRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var vendorId = request.VendorId;
        if (!await db.Vendors.AnyAsync(v => v.Id == vendorId))
        {
            throw ApiException.Validation(new Dictionary<string, string[]>
            {
                ["vendorId"] = new[] { "Vendor was not found in this organisation." }
            });
        }

        if (await db.VendorEventLinks.AnyAsync(l => l.EventId == eventId && l.VendorId == vendorId))
        {
            throw ApiException.Conflict("This vendor is already linked to the event.");
        }

        db.VendorEventLinks.Add(new VendorEventLink
        {
            OrgId = currentUser.OrganizationId,
            EventId = eventId,
            VendorId = vendorId,
            Status = request.Status ?? VendorEventStatus.Contacted
        });
        await db.SaveChangesAsync();

        var response = await LoadLinkAsync(db, eventId, vendorId);
        return Results.Created($"/api/v1/events/{eventId}/vendors/{vendorId}", response);
    }

    private static async Task<EventVendorResponse> UpdateEventVendor(
        Guid eventId,
        Guid vendorId,
        UpdateEventVendorRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var link = await db.VendorEventLinks.SingleOrDefaultAsync(l => l.EventId == eventId && l.VendorId == vendorId)
            ?? throw ApiException.NotFound("Event vendor");

        link.Status = request.Status;
        await db.SaveChangesAsync();

        return await LoadLinkAsync(db, eventId, vendorId);
    }

    private static async Task<IResult> RemoveEventVendor(
        Guid eventId,
        Guid vendorId,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EventEndpoints.RequirePlannerAsync(db, currentUser, eventId);

        var link = await db.VendorEventLinks.SingleOrDefaultAsync(l => l.EventId == eventId && l.VendorId == vendorId)
            ?? throw ApiException.NotFound("Event vendor");

        var linkId = link.Id;
        if (await db.QuoteRequests.AnyAsync(q => q.VendorEventLinkId == linkId))
        {
            throw ApiException.Conflict("This vendor has quote requests for the event and cannot be unlinked.");
        }

        db.VendorEventLinks.Remove(link);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    // ---- Helpers ----

    private static async Task<EventVendorResponse> LoadLinkAsync(EventPlugDbContext db, Guid eventId, Guid vendorId) =>
        await ProjectLinks(db.VendorEventLinks.AsNoTracking().Where(l => l.EventId == eventId && l.VendorId == vendorId))
            .SingleAsync();

    private static IQueryable<VendorResponse> Project(IQueryable<Vendor> query) =>
        query.Select(v => new VendorResponse(v.Id, v.Name, v.Category, v.ContactEmail, v.ContactPhone, v.Notes));

    private static IQueryable<EventVendorResponse> ProjectLinks(IQueryable<VendorEventLink> query) =>
        query.Select(l => new EventVendorResponse(
            l.VendorId, l.EventId, l.Vendor.Name, l.Vendor.Category, l.Vendor.ContactEmail, l.Vendor.ContactPhone, l.Status));

    private static VendorResponse ToResponse(Vendor v) =>
        new(v.Id, v.Name, v.Category, v.ContactEmail, v.ContactPhone, v.Notes);

    private static void ValidateContact(RequestValidator validator, string? email, string? phone, string? notes)
    {
        validator
            .MaxLength(email, "contactEmail", 320)
            .MaxLength(phone, "contactPhone", 50)
            .MaxLength(notes, "notes", 4000)
            .Check(string.IsNullOrWhiteSpace(email) || GuestEndpoints.LooksLikeEmail(email.Trim()),
                "contactEmail", "Must be a valid email address.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
