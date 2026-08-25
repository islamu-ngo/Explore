// ABOUTME: Write DTO for creating session runtime custom property options, used nested within definition DTOs.
// ABOUTME: Mirrors CreateEventCustomPropertyOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public sealed record CreateEventSessionCustomPropertyOptionDto
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
