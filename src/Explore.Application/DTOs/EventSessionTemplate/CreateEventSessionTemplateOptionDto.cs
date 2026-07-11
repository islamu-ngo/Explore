// ABOUTME: Write DTO for creating session template property options, used nested within definition DTOs.
// ABOUTME: Mirrors CreateEventTemplateOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventSessionTemplate;

public class CreateEventSessionTemplateOptionDto
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
