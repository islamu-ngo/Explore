// ABOUTME: Registration-scoped read model for attendee iCalendar exports.
// ABOUTME: Keeps attendee-purpose location values separate from the anonymous calendar contract.

namespace Explore.Application.DTOs.Event;

public sealed record AttendeeEventCalendarExportDto(
    Guid EventId,
    string Title,
    string? Description,
    string? Slug,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Location);
