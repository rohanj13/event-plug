using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Domain.Entities;
using EventPlug.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Endpoints;

public static class TeamEndpoints
{
    private const int NameMaxLength = 200;

    public static RouteGroupBuilder MapTeamEndpoints(this RouteGroupBuilder v1)
    {
        var teams = v1.MapGroup("/teams").WithTags("Teams");

        teams.MapGet("", ListTeams)
            .WithName("listTeams")
            .WithSummary("Teams in the organisation. Set mine=true for only the teams the caller belongs to.");

        teams.MapPost("", CreateTeam)
            .WithName("createTeam")
            .WithSummary("Create a team. Admin only.")
            .Produces<TeamResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict);

        teams.MapGet("/{teamId:guid}", GetTeam)
            .WithName("getTeam")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        teams.MapPatch("/{teamId:guid}", UpdateTeam)
            .WithName("updateTeam")
            .WithSummary("Rename a team. Admin only.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        teams.MapDelete("/{teamId:guid}", DeleteTeam)
            .WithName("deleteTeam")
            .WithSummary("Delete a team and its memberships. Admin only. 409 if any event, expense split or approval still references it.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        var members = teams.MapGroup("/{teamId:guid}/members").WithTags("Team members");

        members.MapGet("", ListMembers)
            .WithName("listTeamMembers")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        members.MapPost("", AddMember)
            .WithName("addTeamMember")
            .WithSummary("Add a user to the team. Admin, or a Planner on this team.")
            .Produces<TeamMemberResponse>(StatusCodes.Status201Created)
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        members.MapPatch("/{userId:guid}", UpdateMember)
            .WithName("updateTeamMember")
            .WithSummary("Change a member's role. Admin, or a Planner on this team. Non-admins cannot demote the last Planner.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        members.MapDelete("/{userId:guid}", RemoveMember)
            .WithName("removeTeamMember")
            .WithSummary("Remove a member. Admin, a Planner on this team, or the member themselves. Non-admins cannot remove the last Planner.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesApiErrors(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        return v1;
    }

    private static async Task<ListResponse<TeamResponse>> ListTeams(
        CurrentUser currentUser,
        EventPlugDbContext db,
        bool? mine)
    {
        var userId = currentUser.UserId;
        var query = db.Teams.AsNoTracking();

        if (mine == true)
        {
            query = query.Where(t => t.Memberships.Any(m => m.UserId == userId));
        }

        var items = await ProjectTeams(query.OrderBy(t => t.Name), userId).ToListAsync();
        return ListResponse.From(items);
    }

    private static async Task<IResult> CreateTeam(
        CreateTeamRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        currentUser.RequireAdmin();
        new RequestValidator().Required(request.Name, "name", NameMaxLength).ThrowIfInvalid();

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(db, name, excludeTeamId: null);

        var team = new Team
        {
            OrgId = currentUser.OrganizationId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        var response = new TeamResponse(team.Id, team.Name, 0, null, team.CreatedAt);
        return Results.Created($"/api/v1/teams/{team.Id}", response);
    }

    private static async Task<TeamResponse> GetTeam(Guid teamId, CurrentUser currentUser, EventPlugDbContext db)
    {
        var userId = currentUser.UserId;
        return await ProjectTeams(db.Teams.AsNoTracking().Where(t => t.Id == teamId), userId).SingleOrDefaultAsync()
            ?? throw ApiException.NotFound("Team");
    }

    private static async Task<TeamResponse> UpdateTeam(
        Guid teamId,
        UpdateTeamRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        currentUser.RequireAdmin();
        new RequestValidator().NotBlankIfProvided(request.Name, "name", NameMaxLength).ThrowIfInvalid();

        var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == teamId)
            ?? throw ApiException.NotFound("Team");

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            await EnsureNameIsFreeAsync(db, name, excludeTeamId: team.Id);
            team.Name = name;
        }

        await db.SaveChangesAsync();
        return await GetTeam(teamId, currentUser, db);
    }

    private static async Task<IResult> DeleteTeam(Guid teamId, CurrentUser currentUser, EventPlugDbContext db)
    {
        currentUser.RequireAdmin();

        var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == teamId)
            ?? throw ApiException.NotFound("Team");

        var inUse =
            await db.EventTeams.AnyAsync(et => et.TeamId == teamId) ||
            await db.ExpenseTeamSplits.AnyAsync(s => s.TeamId == teamId) ||
            await db.Approvals.AnyAsync(a => a.TeamId == teamId);

        if (inUse)
        {
            throw ApiException.Conflict("This team is assigned to events, expense splits or approvals and cannot be deleted.");
        }

        var memberships = await db.TeamMemberships.Where(m => m.TeamId == teamId).ToListAsync();
        db.TeamMemberships.RemoveRange(memberships);
        db.Teams.Remove(team);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<ListResponse<TeamMemberResponse>> ListMembers(Guid teamId, EventPlugDbContext db)
    {
        await EnsureTeamExistsAsync(db, teamId);

        var items = await db.TeamMemberships.AsNoTracking()
            .Where(m => m.TeamId == teamId)
            .OrderBy(m => m.User.Name)
            .Select(m => new TeamMemberResponse(m.UserId, m.User.Name, m.User.Email, m.TeamRole))
            .ToListAsync();

        return ListResponse.From(items);
    }

    private static async Task<IResult> AddMember(
        Guid teamId,
        AddTeamMemberRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        new RequestValidator().Check(request.UserId != Guid.Empty, "userId", "This field is required.").ThrowIfInvalid();

        await EnsureTeamExistsAsync(db, teamId);
        await currentUser.RequireTeamRoleAsync(teamId, TeamRole.Planner);

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.UserId)
            ?? throw ApiException.NotFound("User");

        if (await db.TeamMemberships.AnyAsync(m => m.TeamId == teamId && m.UserId == request.UserId))
        {
            throw ApiException.Conflict("This user is already a member of the team.");
        }

        db.TeamMemberships.Add(new TeamMembership
        {
            OrgId = currentUser.OrganizationId,
            TeamId = teamId,
            UserId = user.Id,
            TeamRole = request.Role
        });
        await db.SaveChangesAsync();

        var response = new TeamMemberResponse(user.Id, user.Name, user.Email, request.Role);
        return Results.Created($"/api/v1/teams/{teamId}/members/{user.Id}", response);
    }

    private static async Task<TeamMemberResponse> UpdateMember(
        Guid teamId,
        Guid userId,
        UpdateTeamMemberRequest request,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EnsureTeamExistsAsync(db, teamId);
        await currentUser.RequireTeamRoleAsync(teamId, TeamRole.Planner);

        var membership = await FindMembershipAsync(db, teamId, userId);

        if (membership.TeamRole == TeamRole.Planner && request.Role != TeamRole.Planner)
        {
            await EnsureNotLastPlannerAsync(db, currentUser, teamId, userId);
        }

        membership.TeamRole = request.Role;
        await db.SaveChangesAsync();

        return new TeamMemberResponse(membership.UserId, membership.User.Name, membership.User.Email, membership.TeamRole);
    }

    private static async Task<IResult> RemoveMember(
        Guid teamId,
        Guid userId,
        CurrentUser currentUser,
        EventPlugDbContext db)
    {
        await EnsureTeamExistsAsync(db, teamId);

        if (userId != currentUser.UserId)
        {
            await currentUser.RequireTeamRoleAsync(teamId, TeamRole.Planner);
        }

        var membership = await FindMembershipAsync(db, teamId, userId);

        if (membership.TeamRole == TeamRole.Planner)
        {
            await EnsureNotLastPlannerAsync(db, currentUser, teamId, userId);
        }

        db.TeamMemberships.Remove(membership);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static IQueryable<TeamResponse> ProjectTeams(IQueryable<Team> query, Guid userId) =>
        query.Select(t => new TeamResponse(
            t.Id,
            t.Name,
            t.Memberships.Count,
            t.Memberships.Where(m => m.UserId == userId).Select(m => (TeamRole?)m.TeamRole).FirstOrDefault(),
            t.CreatedAt));

    private static async Task EnsureTeamExistsAsync(EventPlugDbContext db, Guid teamId)
    {
        if (!await db.Teams.AnyAsync(t => t.Id == teamId))
        {
            throw ApiException.NotFound("Team");
        }
    }

    private static async Task EnsureNameIsFreeAsync(EventPlugDbContext db, string name, Guid? excludeTeamId)
    {
        var lowered = name.ToLower();
        if (await db.Teams.AnyAsync(t => t.Name.ToLower() == lowered && t.Id != excludeTeamId))
        {
            throw ApiException.Conflict($"A team named \"{name}\" already exists.");
        }
    }

    private static async Task<TeamMembership> FindMembershipAsync(EventPlugDbContext db, Guid teamId, Guid userId) =>
        await db.TeamMemberships
            .Include(m => m.User)
            .SingleOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId)
            ?? throw ApiException.NotFound("Team member");

    // Admins may leave a team without a Planner; everyone else must hand over first so the team is never orphaned.
    private static async Task EnsureNotLastPlannerAsync(EventPlugDbContext db, CurrentUser currentUser, Guid teamId, Guid userId)
    {
        if (currentUser.IsAdmin)
        {
            return;
        }

        var otherPlanners = await db.TeamMemberships.AnyAsync(m =>
            m.TeamId == teamId && m.UserId != userId && m.TeamRole == TeamRole.Planner);

        if (!otherPlanners)
        {
            throw ApiException.Conflict("A team must keep at least one Planner. Promote another member first.");
        }
    }
}
