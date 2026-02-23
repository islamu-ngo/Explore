// ABOUTME: DTO that groups tags by their tag type for the tri-state tag filter dropdown.
// Used by GetTagsGroupedByTagTypeRequest to return tags organized by category.

using Explore.Application.DTOs.Tag;

namespace Explore.Application.DTOs.TagType;

public class TagTypeWithTagsDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public List<TagListDto> Tags { get; set; } = [];
}
