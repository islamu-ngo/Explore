// ABOUTME: Grouped update DTO for event-to-tag link mutations.
// ABOUTME: Nullable groups allow callers to update the event side or tag side independently.

using System;

namespace Explore.Application.DTOs.EventTags;

public sealed record UpdateEventTagsDto
{
    public UpdateEventTagsEventDto? Event { get; init; }
    public UpdateEventTagsTagDto? Tag { get; init; }
}

public sealed record UpdateEventTagsEventDto
{
    public Guid EventId { get; init; }
}

public sealed record UpdateEventTagsTagDto
{
    public Guid TagId { get; init; }
}
