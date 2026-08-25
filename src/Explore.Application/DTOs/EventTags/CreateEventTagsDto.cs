using System;

namespace Explore.Application.DTOs.EventTags;

public sealed record CreateEventTagsDto
{
    public Guid EventId { get; init; }
    public Guid TagId { get; init; }
    public Guid TenantId { get; init; }
}
