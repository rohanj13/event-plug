using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EventPlug.Api.Tenancy;

/// <summary>
/// Password-less "log in as a persona" mode for a single sample organisation. The API issues its
/// own HS256 tokens instead of Auth0, and X-Org-Id falls back to <see cref="OrgId"/>.
/// Anyone who can reach the API can sign in, so only enable this for throwaway demo data.
/// </summary>
public sealed class DemoAuthOptions
{
    public const string Section = "DemoAuth";
    public const string Issuer = "eventplug-demo";
    public const string Audience = "eventplug-demo";
    public const string SubjectPrefix = "demo|";

    public bool Enabled { get; set; }
    public string SigningKey { get; set; } = string.Empty;
    public Guid OrgId { get; set; } = DemoIds.Organization;
    public bool SeedOnStartup { get; set; } = true;
    public int TokenLifetimeHours { get; set; } = 12;

    public SymmetricSecurityKey GetSecurityKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public void Validate()
    {
        if (Enabled && Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException("DemoAuth:SigningKey must be at least 32 characters when DemoAuth is enabled.");
        }
    }

    public TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = GetSecurityKey(),
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    public (string Token, DateTimeOffset ExpiresAt) IssueToken(string subject, string name, string email)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(TokenLifetimeHours);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = expiresAt.UtcDateTime,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", subject),
                new Claim("name", name),
                new Claim("email", email)
            }),
            SigningCredentials = new SigningCredentials(GetSecurityKey(), SecurityAlgorithms.HmacSha256)
        });

        return (token, expiresAt);
    }
}

public static class DemoIds
{
    public static readonly Guid Organization = Guid.Parse("0d3e6a10-0000-4000-8000-000000000001");
}
