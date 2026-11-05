using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Contracts;

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    SubscriptionTier SubscriptionTier,
    DateOnly BillingCycleStart,
    DateTimeOffset CreatedAt);

/// <summary>Only the name is editable by the client; billing fields are managed by EventPlug.</summary>
public sealed record UpdateOrganizationRequest(string? Name);

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    OrgRole OrgRole);

public sealed record UserTeamMembershipResponse(
    Guid TeamId,
    string TeamName,
    TeamRole Role);

public sealed record UserDetailResponse(
    Guid Id,
    string Name,
    string Email,
    OrgRole OrgRole,
    IReadOnlyList<UserTeamMembershipResponse> Teams);

/// <summary>Name can be changed by the user themselves or an admin; OrgRole only by an admin.</summary>
public sealed record UpdateUserRequest(string? Name, OrgRole? OrgRole);

public sealed record MeResponse(
    Guid Id,
    string Name,
    string Email,
    OrgRole OrgRole,
    OrganizationResponse Organization,
    IReadOnlyList<UserTeamMembershipResponse> Teams);
