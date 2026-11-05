using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EventPlug.Api.Tenancy;

/// <summary>
/// Development-only stand-in for Auth0: every request is signed in as <see cref="Subject"/>,
/// and X-Org-Id falls back to <see cref="OrgId"/> when the header is missing.
/// </summary>
public sealed class DevAuthOptions
{
    public const string Section = "DevAuth";
    public const string Scheme = "DevAuth";

    public bool Enabled { get; set; }
    public string Subject { get; set; } = string.Empty;
    public Guid? OrgId { get; set; }
}

public sealed class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly DevAuthOptions devAuth;

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<DevAuthOptions> devAuth)
        : base(options, logger, encoder)
    {
        this.devAuth = devAuth.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", devAuth.Subject) }, DevAuthOptions.Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), DevAuthOptions.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
