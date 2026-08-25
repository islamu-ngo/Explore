// ABOUTME: Write DTO for creating event runtime custom property options.
// ABOUTME: Used when adding options to event-local definitions created without a template.

namespace Explore.Application.DTOs.EventCustomProperty;

public sealed record CreateEventCustomPropertyOptionDto
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
