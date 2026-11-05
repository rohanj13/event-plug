using EventPlug.Api.Common;
using EventPlug.Api.Contracts;
using EventPlug.Api.Data;
using EventPlug.Api.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventPlug.Api.Endpoints;

public static class DemoAuthEndpoints
{
    public static RouteGroupBuilder MapDemoAuthEndpoints(this RouteGroupBuilder v1)
    {
        var auth = v1.MapGroup("/auth").WithTags("Demo login").AllowAnonymous();

        auth.MapGet("/demo-users", ListPersonas)
            .WithName("listDemoUsers")
            .WithSummary("Demo personas that can sign in without a password. Only available when demo mode is on.")
            .ProducesApiErrors(StatusCodes.Status404NotFound);

        auth.MapPost("/demo-login", Login)
            .WithName("demoLogin")
            .WithSummary("Sign in as a demo persona. Send the returned accessToken as 'Authorization: Bearer <token>'; X-Org-Id is optional in demo mode.")
            .ProducesApiErrors(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound);

        return v1;
    }

    private static async Task<ListResponse<DemoPersonaResponse>> ListPersonas(
        EventPlugDbContext db,
        IOptions<DemoAuthOptions> options,
        CancellationToken cancellationToken)
    {
        var demo = RequireEnabled(options);
        var items = await LoadPersonasAsync(db, demo.OrgId, cancellationToken);
        return new ListResponse<DemoPersonaResponse>(items, items.Count);
    }

    private static async Task<DemoLoginResponse> Login(
        DemoLoginRequest request,
        EventPlugDbContext db,
        IOptions<DemoAuthOptions> options,
        CancellationToken cancellationToken)
    {
        var demo = RequireEnabled(options);

        if (string.IsNullOrWhiteSpace(request.Persona))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "validation_failed", "Choose a persona.",
                new Dictionary<string, string[]> { ["persona"] = new[] { "Persona is required." } });
        }

        var personas = await LoadPersonasAsync(db, demo.OrgId, cancellationToken);
        var persona = personas.FirstOrDefault(p => p.Persona.Equals(request.Persona.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ApiException(StatusCodes.Status404NotFound, "persona_not_found", $"No demo persona called '{request.Persona}'.");

        var (token, expiresAt) = demo.IssueToken(DemoAuthOptions.SubjectPrefix + persona.Persona, persona.Name, persona.Email);
        return new DemoLoginResponse(token, "Bearer", expiresAt, demo.OrgId, persona);
    }

    private static DemoAuthOptions RequireEnabled(IOptions<DemoAuthOptions> options) =>
        options.Value.Enabled
            ? options.Value
            : throw new ApiException(StatusCodes.Status404NotFound, "demo_disabled", "Demo login is not enabled on this server.");

    private static async Task<List<DemoPersonaResponse>> LoadPersonasAsync(EventPlugDbContext db, Guid orgId, CancellationToken cancellationToken)
    {
        var users = await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrgId == orgId && u.Auth0Subject.StartsWith(DemoAuthOptions.SubjectPrefix))
            .Select(u => new
            {
                u.Auth0Subject,
                u.Name,
                u.Email,
                u.OrgRole,
                Teams = u.TeamMemberships
                    .OrderBy(m => m.Team.Name)
                    .Select(m => new DemoPersonaTeamResponse(m.Team.Name, m.TeamRole.ToString()))
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var descriptions = DemoDataSeeder.Personas.ToDictionary(DemoDataSeeder.SubjectFor, p => p.Description);
        var order = DemoDataSeeder.Personas.Select((p, index) => (p.Key, index)).ToDictionary(x => x.Key, x => x.index);

        return users
            .Select(u => new DemoPersonaResponse(
                u.Auth0Subject[DemoAuthOptions.SubjectPrefix.Length..],
                u.Name,
                u.Email,
                u.OrgRole.ToString(),
                descriptions.GetValueOrDefault(u.Auth0Subject, string.Empty),
                u.Teams))
            .OrderBy(p => order.TryGetValue(p.Persona, out var index) ? index : int.MaxValue)
            .ToList();
    }
}
