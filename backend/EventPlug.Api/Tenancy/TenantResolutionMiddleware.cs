using System.Security.Claims;
using EventPlug.Api.Common;
using EventPlug.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventPlug.Api.Tenancy;

public sealed class TenantResolutionMiddleware
{
    public const string OrganizationHeader = "X-Org-Id";
    private readonly RequestDelegate next;
    private readonly Guid? defaultOrganizationId;

    public TenantResolutionMiddleware(RequestDelegate next, IOptions<DevAuthOptions> devAuth, IOptions<DemoAuthOptions> demoAuth)
    {
        this.next = next;
        defaultOrganizationId =
            devAuth.Value.Enabled ? devAuth.Value.OrgId :
            demoAuth.Value.Enabled ? demoAuth.Value.OrgId :
            null;
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
            await WriteErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "unauthorized", "A valid bearer token is required.");
            return;
        }

        Guid organizationId;
        if (httpContext.Request.Headers.TryGetValue(OrganizationHeader, out var headerValue))
        {
            if (!Guid.TryParse(headerValue.ToString(), out organizationId))
            {
                await WriteErrorAsync(httpContext, StatusCodes.Status400BadRequest, "missing_org", $"A valid {OrganizationHeader} header is required.");
                return;
            }
        }
        else if (defaultOrganizationId is { } fallback)
        {
            organizationId = fallback;
        }
        else
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status400BadRequest, "missing_org", $"A valid {OrganizationHeader} header is required.");
            return;
        }

        var auth0Subject = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(auth0Subject))
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status401Unauthorized, "unauthorized", "The token has no subject claim.");
            return;
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(user =>
                user.OrgId == organizationId &&
                user.Auth0Subject == auth0Subject);

        if (user is null)
        {
            await WriteErrorAsync(httpContext, StatusCodes.Status403Forbidden, "not_a_member", "You are not a member of this organisation.");
            return;
        }

        tenantContext.SetMembership(organizationId, user.Id, user.OrgRole);
        await next(httpContext);
    }

    private static bool IsAnonymousEndpoint(PathString path)
    {
        return path.StartsWithSegments("/api/health") ||
               path.StartsWithSegments("/api/db-check") ||
               path.StartsWithSegments("/api/v1/auth") ||
               path.StartsWithSegments("/swagger");
    }

    private static Task WriteErrorAsync(HttpContext httpContext, int statusCode, string code, string message)
    {
        httpContext.Response.StatusCode = statusCode;
        return httpContext.Response.WriteAsJsonAsync(new ApiError(code, message));
    }
}
