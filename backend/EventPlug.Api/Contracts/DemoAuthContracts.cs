namespace EventPlug.Api.Contracts;

public sealed record DemoPersonaTeamResponse(string TeamName, string Role);

public sealed record DemoPersonaResponse(
    string Persona,
    string Name,
    string Email,
    string OrgRole,
    string Description,
    IReadOnlyList<DemoPersonaTeamResponse> Teams);

public sealed record DemoLoginRequest(string Persona);

public sealed record DemoLoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    Guid OrgId,
    DemoPersonaResponse User);
