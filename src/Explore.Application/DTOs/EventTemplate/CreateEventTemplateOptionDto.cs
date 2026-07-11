// ABOUTME: Write DTO for creating template property options, used nested within CreateEventTemplateDefinitionDto.
// ABOUTME: Mirrors CreateCustomPropertyOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventTemplate;

public class CreateEventTemplateOptionDto
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
