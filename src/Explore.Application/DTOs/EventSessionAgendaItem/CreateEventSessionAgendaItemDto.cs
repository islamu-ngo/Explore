using System;

namespace Explore.Application.DTOs.EventSessionAgendaItem;

public sealed record CreateEventSessionAgendaItemDto
{
    public Guid EventSessionId { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public Guid? LocationId { get; init; }
}
