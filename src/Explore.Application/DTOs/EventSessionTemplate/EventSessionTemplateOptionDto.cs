// ABOUTME: Read-only DTO for session template property options, returned nested within definition DTOs.
// ABOUTME: Mirrors EventTemplateOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventSessionTemplate;

public sealed record EventSessionTemplateOptionDto
{
    public Guid Id { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Value { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}
