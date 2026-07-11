// ABOUTME: Read model for public iCalendar exports of published events.
// ABOUTME: Keeps calendar endpoint data selection in Application while API owns file serialization.

namespace Explore.Application.DTOs.Event;

public sealed record EventCalendarExportDto(
    Guid EventId,
    string Title,
    string? Description,
    string? Slug,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? Location);
