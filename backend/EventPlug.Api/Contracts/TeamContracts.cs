using System.Text.Json.Serialization;
using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Contracts;

/// <summary>MyRole is the caller's role on this team, or null if they are not a member.</summary>
public sealed record TeamResponse(
    Guid Id,
    string Name,
    int MemberCount,
    TeamRole? MyRole,
    DateTimeOffset CreatedAt);

public sealed record CreateTeamRequest(string Name);

public sealed record UpdateTeamRequest(string? Name);

public sealed record TeamMemberResponse(
    Guid UserId,
    string Name,
    string Email,
    TeamRole Role);

public sealed record AddTeamMemberRequest(
    [property: JsonRequired] Guid UserId,
    [property: JsonRequired] TeamRole Role);

public sealed record UpdateTeamMemberRequest([property: JsonRequired] TeamRole Role);
