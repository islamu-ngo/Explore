// ABOUTME: Write DTO for creating template property options, used nested within CreateEventTemplateDefinitionDto.
// ABOUTME: Mirrors CreateCustomPropertyOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventTemplate;

public sealed record CreateEventTemplateOptionDto
{
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Value { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
}
