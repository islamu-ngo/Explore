// ABOUTME: DTO that groups tags by their tag type for the tri-state tag filter dropdown.
// Used by GetTagsGroupedByTagTypeRequest to return tags organized by category.

using Explore.Application.DTOs.Tag;

namespace Explore.Application.DTOs.TagType;

public sealed record TagTypeWithTagsDto
{
    public int Id { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
    public List<TagListDto> Tags { get; init; } = [];
}
