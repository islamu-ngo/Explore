// ABOUTME: Read DTO for a selectable option belonging to a shared Layer 3 custom-property definition.
// ABOUTME: Exposes machine identity and display metadata for admin/query flows.

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public sealed record CustomPropertyOptionDto
{
    public Guid Id { get; init; }
    public required string Namespace { get; init; }
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string Value { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
