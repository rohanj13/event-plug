namespace EventPlug.Api.Contracts;

/// <summary>Day is 1-based within the event (1 for a single-day event). StartTime/EndTime are full timestamps.</summary>
public sealed record RunSheetItemResponse(
    Guid Id,
    Guid EventId,
    int Day,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    string Title,
    string? Description,
    Guid? OwnerUserId,
    string? OwnerName,
    int SortOrder);

/// <summary>Day defaults to 1. SortOrder defaults to the end of that day's list.</summary>
public sealed record CreateRunSheetItemRequest(
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    int? Day,
    string? Description,
    Guid? OwnerUserId,
    int? SortOrder);

/// <summary>Omitted (null) fields are left unchanged.</summary>
public sealed record UpdateRunSheetItemRequest(
    string? Title,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    int? Day,
    string? Description,
    Guid? OwnerUserId,
    int? SortOrder);
