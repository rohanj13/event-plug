using System.Text.Json.Serialization;
using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Contracts;

public sealed record GuestResponse(
    Guid Id,
    Guid EventId,
    string Name,
    string Email,
    RsvpStatus RsvpStatus,
    string? DietaryNotes,
    string? AccessNotes);

/// <summary>RsvpStatus defaults to Invited.</summary>
public sealed record CreateGuestRequest(
    string Name,
    string Email,
    RsvpStatus? RsvpStatus,
    string? DietaryNotes,
    string? AccessNotes);

/// <summary>Omitted (null) fields are left unchanged.</summary>
public sealed record UpdateGuestRequest(
    string? Name,
    string? Email,
    RsvpStatus? RsvpStatus,
    string? DietaryNotes,
    string? AccessNotes);

public sealed record BulkGuestsRequest([property: JsonRequired] IReadOnlyList<CreateGuestRequest> Guests);

public sealed record BulkGuestError(int Row, string Message);

/// <summary>Row numbers are 1-based positions in the submitted list. Skipped rows are duplicates by email.</summary>
public sealed record BulkGuestsResponse(
    int Created,
    int Skipped,
    IReadOnlyList<BulkGuestError> Errors);

public sealed record GuestSummaryResponse(
    int Total,
    int Invited,
    int Yes,
    int No,
    int Maybe);
