// ABOUTME: Read DTO for a selectable option belonging to a shared Layer 3 custom-property definition.
// ABOUTME: Exposes machine identity and display metadata for admin/query flows.

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public class CustomPropertyOptionDto
{
    public Guid Id { get; set; }
    public required string Namespace { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string Value { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
