// ABOUTME: Read-only DTO for session template property options, returned nested within definition DTOs.
// ABOUTME: Mirrors EventTemplateOptionDto shape for consistency across the EAV system.

namespace Explore.Application.DTOs.EventSessionTemplate;

public class EventSessionTemplateOptionDto
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
