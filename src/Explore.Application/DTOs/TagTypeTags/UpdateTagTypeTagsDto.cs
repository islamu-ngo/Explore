// ABOUTME: Grouped Application-only update contract for a tag-to-tag-type relationship.
// ABOUTME: Keeps junction identity server-owned while allowing either relationship endpoint to change.

namespace Explore.Application.DTOs.TagTypeTags;

public class UpdateTagTypeTagsDto
{
    public UpdateTagTypeTagsRelationshipDto? Relationship { get; set; }
}

public sealed class UpdateTagTypeTagsRelationshipDto
{
    public Guid? TagId { get; set; }
    public int? TagTypeId { get; set; }
}
