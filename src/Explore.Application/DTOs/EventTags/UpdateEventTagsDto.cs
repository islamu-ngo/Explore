// ABOUTME: Grouped update DTO for event-to-tag link mutations.
// ABOUTME: Nullable groups allow callers to update the event side or tag side independently.

using System;

namespace Explore.Application.DTOs.EventTags;

public class UpdateEventTagsDto
{
    public UpdateEventTagsEventDto? Event { get; set; }
    public UpdateEventTagsTagDto? Tag { get; set; }
}

public class UpdateEventTagsEventDto
{
    public Guid EventId { get; set; }
}

public class UpdateEventTagsTagDto
{
    public Guid TagId { get; set; }
}
