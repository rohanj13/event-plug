using System.Text.Json.Serialization;
using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Contracts;

public sealed record EventTeamResponse(
    Guid TeamId,
    string TeamName,
    bool IsOwningTeam);

public sealed record EventResponse(
    Guid Id,
    string Title,
    DateOnly EventDate,
    DateOnly? EndDate,
    string Location,
    EventStatus Status,
    EventScope Scope,
    int? GuestCountEstimate,
    decimal? BudgetAmount,
    string? BudgetBand,
    string? StyleNotes,
    bool IsShared,
    Guid OwningTeamId,
    IReadOnlyList<EventTeamResponse> Teams,
    Guid CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt);

/// <summary>OwningTeamId must be a team the caller plans for (or any team, for admins). Status defaults to Draft, scope to Team.</summary>
public sealed record CreateEventRequest(
    string Title,
    DateOnly EventDate,
    DateOnly? EndDate,
    string Location,
    [property: JsonRequired] Guid OwningTeamId,
    EventScope? Scope,
    int? GuestCountEstimate,
    decimal? BudgetAmount,
    string? BudgetBand,
    string? StyleNotes,
    EventStatus? Status,
    bool? IsShared);

/// <summary>Omitted (null) fields are left unchanged.</summary>
public sealed record UpdateEventRequest(
    string? Title,
    DateOnly? EventDate,
    DateOnly? EndDate,
    string? Location,
    EventScope? Scope,
    int? GuestCountEstimate,
    decimal? BudgetAmount,
    string? BudgetBand,
    string? StyleNotes,
    EventStatus? Status,
    bool? IsShared);

public sealed record AddEventTeamRequest([property: JsonRequired] Guid TeamId);
