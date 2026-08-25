using System;

namespace Explore.Application.DTOs.TagTypeTags;

public sealed record CreateTagTypeTagsDto
{
    public Guid TagId { get; init; }
    public int TagTypeId { get; init; }
    public Guid TenantId { get; init; }
}
