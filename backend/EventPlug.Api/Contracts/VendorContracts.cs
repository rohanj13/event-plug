using System.Text.Json.Serialization;
using EventPlug.Api.Domain.Enums;

namespace EventPlug.Api.Contracts;

public sealed record VendorResponse(
    Guid Id,
    string Name,
    string Category,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes);

/// <summary>Category is free text, e.g. Catering, Venue, AV.</summary>
public sealed record CreateVendorRequest(
    string Name,
    string Category,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes);

/// <summary>Omitted (null) fields are left unchanged.</summary>
public sealed record UpdateVendorRequest(
    string? Name,
    string? Category,
    string? ContactEmail,
    string? ContactPhone,
    string? Notes);

public sealed record EventVendorResponse(
    Guid VendorId,
    Guid EventId,
    string Name,
    string Category,
    string? ContactEmail,
    string? ContactPhone,
    VendorEventStatus Status);

/// <summary>Status defaults to Contacted.</summary>
public sealed record AddEventVendorRequest(
    [property: JsonRequired] Guid VendorId,
    VendorEventStatus? Status);

public sealed record UpdateEventVendorRequest(
    [property: JsonRequired] VendorEventStatus Status);
