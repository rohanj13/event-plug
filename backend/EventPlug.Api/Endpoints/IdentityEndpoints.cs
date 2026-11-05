using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class IdentityEndpoints
{
    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapGet("/me", GetMe)
            .WithName("getMe")
            .WithTags("Me")
            .WithSummary("The signed-in user, their organisation and their team memberships.");

        var organization = v1.MapGroup("/organization").WithTags("Organization");

        organization.MapGet("", GetOrganization)
            .WithName("getOrganization")
            .WithSummary("The organisation selected by the X-Org-Id header.");

        organization.MapPatch("", UpdateOrganization)
            .WithName("updateOrganization")
            .WithSummary("Update the organisation. Admin only.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest);

        var users = v1.MapGroup("/users").WithTags("Users");

        users.MapGet("", ListUsers)
            .WithName("listUsers")
            .WithSummary("Users in the organisation. Optional search on name/email and filter by team.");

        users.MapGet("/{userId:guid}", GetUser)
            .WithName("getUser")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        users.MapPatch("/{userId:guid}", UpdateUser)
            .WithName("updateUser")
            .WithSummary("Users can rename themselves; admins can rename anyone and change OrgRole.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return v1;
    }

    private static async Task<MeResponse> GetMe(CurrentUser currentUser, EventPlugDbContext db)
    {
        var userId = currentUser.UserId;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw ApiException.NotFound("User");
        var organization = await LoadOrganizationAsync(currentUser, db);
        var teams = await LoadMembershipsAsync(db, user.Id);

        return new MeResponse(user.Id, user.Name, user.Email, user.OrgRole, ToResponse(organization), teams);
    }

    private static async Task<OrganizationResponse> GetOrganization(CurrentUser currentUser, EventPlugDbContext db) =>
        ToResponse(await LoadOrganizationAsync(currentUser, db));

    private static async Task<OrganizationResponse> UpdateOrganization(
        UpdateOrganizationRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        currentUser.RequireAdmin();
        new RequestValidator().NotBlankIfProvided(request.Name, "name", 200).ThrowIfInvalid();

        var organizationId = currentUser.OrganizationId;
        var organization = await db.Organizations.SingleOrDefaultAsync(o => o.Id == organizationId)
            ?? throw ApiException.NotFound("Organization");

        if (request.Name is not null)
        {
            organization.Name = request.Name.Trim();
        }

        await db.SaveChangesAsync();
        return ToResponse(organization);
    }

    private static async Task<ListResponse<UserResponse>> ListUsers(
        EventPlugDbContext db,
        string? search,
        Guid? teamId)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        if (teamId is not null)
        {
            query = query.Where(u => u.TeamMemberships.Any(m => m.TeamId == teamId));
        }

        var items = await query
            .OrderBy(u => u.Name)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.OrgRole))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<UserDetailResponse> GetUser(Guid userId, EventPlugDbContext db)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw ApiException.NotFound("User");

        return new UserDetailResponse(user.Id, user.Name, user.Email, user.OrgRole, await LoadMembershipsAsync(db, user.Id));
    }

    private static async Task<UserDetailResponse> UpdateUser(
        Guid userId,
        UpdateUserRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        if (userId != currentUser.UserId || request.OrgRole is not null)
        {
            currentUser.RequireAdmin();
        }

        new RequestValidator().NotBlankIfProvided(request.Name, "name", 200).ThrowIfInvalid();

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw ApiException.NotFound("User");

        if (request.OrgRole is { } newRole && newRole != user.OrgRole)
        {
            if (user.OrgRole == OrgRole.Admin &&
                !await db.Users.AnyAsync(u => u.Id != user.Id && u.OrgRole == OrgRole.Admin))
            {
                throw ApiException.Conflict("The organisation must keep at least one admin.");
            }

            user.OrgRole = newRole;
        }

        if (request.Name is not null)
        {
            user.Name = request.Name.Trim();
        }

        await db.SaveChangesAsync();
        return new UserDetailResponse(user.Id, user.Name, user.Email, user.OrgRole, await LoadMembershipsAsync(db, user.Id));
    }

    private static async Task<Organization> LoadOrganizationAsync(CurrentUser currentUser, EventPlugDbContext db)
    {
        var organizationId = currentUser.OrganizationId;
        return await db.Organizations.AsNoTracking().SingleOrDefaultAsync(o => o.Id == organizationId)
            ?? throw ApiException.NotFound("Organization");
    }

    private static async Task<IReadOnlyList<UserTeamMembershipResponse>> LoadMembershipsAsync(EventPlugDbContext db, Guid userId) =>
        await db.TeamMemberships.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Team.Name)
            .Select(m => new UserTeamMembershipResponse(m.TeamId, m.Team.Name, m.TeamRole))
            .ToListAsync();

    private static OrganizationResponse ToResponse(Organization o) =>
        new(o.Id, o.Name, o.SubscriptionTier, o.BillingCycleStart, o.CreatedAt);
}
