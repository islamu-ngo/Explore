// ABOUTME: Grouped Application-only update contract for a tag-to-tag-type relationship.
// ABOUTME: Keeps junction identity server-owned while allowing either relationship endpoint to change.

namespace Explore.Application.DTOs.TagTypeTags;

public sealed record UpdateTagTypeTagsDto
{
    public UpdateTagTypeTagsRelationshipDto? Relationship { get; init; }
}

public sealed record UpdateTagTypeTagsRelationshipDto
{
    public Guid? TagId { get; init; }
    public int? TagTypeId { get; init; }
}
