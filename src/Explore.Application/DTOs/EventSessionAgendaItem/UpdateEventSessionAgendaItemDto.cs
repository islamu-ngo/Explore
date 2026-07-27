// ABOUTME: Grouped PATCH contract for a session agenda item.
// ABOUTME: Keeps identity server-owned while allowing sparse relationship, content, schedule, and location changes.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventSessionAgendaItem;

public class UpdateEventSessionAgendaItemDto
{
    public UpdateEventSessionAgendaItemRelationshipDto? Relationship { get; set; }
    public UpdateEventSessionAgendaItemContentDto? Content { get; set; }
    public UpdateEventSessionAgendaItemScheduleDto? Schedule { get; set; }
    public UpdateEventSessionAgendaItemLocationDto? Location { get; set; }
}

public sealed class UpdateEventSessionAgendaItemRelationshipDto
{
    public Guid EventSessionId { get; set; }
}

public sealed class UpdateEventSessionAgendaItemContentDto
{
    public string? Title { get; set; }
    public OptionalUpdate<string?> Description { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventSessionAgendaItemScheduleDto
{
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
}

public sealed class UpdateEventSessionAgendaItemLocationDto
{
    public OptionalUpdate<Guid?> Value { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}
