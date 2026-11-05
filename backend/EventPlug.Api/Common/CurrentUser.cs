using EventPlug.Api.Data;
using EventPlug.Api.Domain.Enums;
using EventPlug.Api.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Common;

/// <summary>
/// The signed-in user for this request. Org Admins pass every check; everyone else is authorised
/// through their role on the relevant team (Planner, Approver, Viewer).
/// </summary>
public sealed class CurrentUser
{
    private readonly ITenantContext tenantContext;
    private readonly EventPlugDbContext dbContext;

    public CurrentUser(ITenantContext tenantContext, EventPlugDbContext dbContext)
    {
        this.tenantContext = tenantContext;
        this.dbContext = dbContext;
    }

    public Guid UserId => tenantContext.UserId ?? throw ApiException.Unauthorized();

    public Guid OrganizationId => tenantContext.OrganizationId ?? throw ApiException.Unauthorized();

    public bool IsAdmin => tenantContext.OrganizationRole == OrgRole.Admin;

    public void RequireAdmin()
    {
        if (!IsAdmin)
        {
            throw ApiException.Forbidden("Only organisation admins can perform this action.");
        }
    }

    public async Task<bool> HasTeamRoleAsync(Guid teamId, params TeamRole[] roles)
    {
        if (IsAdmin)
        {
            return true;
        }

        var userId = UserId;
        return await dbContext.TeamMemberships.AnyAsync(m =>
            m.TeamId == teamId &&
            m.UserId == userId &&
            (roles.Length == 0 || roles.Contains(m.TeamRole)));
    }

    /// <summary>For org-wide records (e.g. vendors) that any planner may maintain.</summary>
    public async Task RequirePlannerOnAnyTeamAsync()
    {
        if (IsAdmin)
        {
            return;
        }

        var userId = UserId;
        if (!await dbContext.TeamMemberships.AnyAsync(m => m.UserId == userId && m.TeamRole == TeamRole.Planner))
        {
            throw ApiException.Forbidden("You need the Planner role on at least one team.");
        }
    }

    public async Task RequireTeamRoleAsync(Guid teamId, params TeamRole[] roles)
    {
        if (!await HasTeamRoleAsync(teamId, roles))
        {
            throw ApiException.Forbidden(roles.Length == 0
                ? "You must be a member of this team."
                : $"You need the {string.Join(" or ", roles)} role on this team.");
        }
    }
}
