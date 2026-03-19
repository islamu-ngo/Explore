// ABOUTME: Write DTO for one selectable option inside a shared Layer 3 custom-property definition.
// ABOUTME: Uses namespaced machine identity so option labels stay mutable without breaking semantics.

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public class CreateCustomPropertyOptionDto
{
    public required string Namespace { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public required string Value { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
