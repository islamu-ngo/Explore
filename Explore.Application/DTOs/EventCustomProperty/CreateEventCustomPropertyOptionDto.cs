// ABOUTME: Write DTO for creating event runtime custom property options.
// ABOUTME: Used when adding options to event-local definitions created without a template.

namespace Explore.Application.DTOs.EventCustomProperty;

public class CreateEventCustomPropertyOptionDto
{
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
