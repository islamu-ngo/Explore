// ABOUTME: Write DTO for creating session runtime custom property options, used nested within definition DTOs.
// ABOUTME: Mirrors CreateEventCustomPropertyOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public class CreateEventSessionCustomPropertyOptionDto
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
