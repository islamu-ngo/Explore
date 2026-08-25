// ABOUTME: Write DTO for creating session template property options, used nested within definition DTOs.
// ABOUTME: Mirrors CreateEventTemplateOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventSessionTemplate;

public sealed record CreateEventSessionTemplateOptionDto
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
