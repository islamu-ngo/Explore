// ABOUTME: Read-only DTO for template property options, returned nested within EventTemplateDefinitionDto.
// ABOUTME: Mirrors CustomPropertyOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventTemplate;

public class EventTemplateOptionDto
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
