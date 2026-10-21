using System.Security.Claims;
using EventPlug.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EventPlug.Api.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private const string OrganizationHeader = "X-Organization-Id";
    private readonly RequestDelegate next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        EventPlugDbContext dbContext,
        ITenantContext tenantContext)
    {
        if (IsAnonymousEndpoint(httpContext.Request.Path))
        {
            await next(httpContext);
            return;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(OrganizationHeader, out var headerValue) ||
            !Guid.TryParse(headerValue.ToString(), out var organizationId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = $"A valid {OrganizationHeader} header is required."
            });
            return;
        }

        var auth0Subject = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(auth0Subject))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user =>
                user.OrgId == organizationId &&
                user.Auth0Subject == auth0Subject);

        if (user is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        tenantContext.SetMembership(organizationId, user.Id, user.OrgRole);
        await next(httpContext);
    }

    private static bool IsAnonymousEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/health") ||
               path.StartsWithSegments("/api/db-check");
    }
}
