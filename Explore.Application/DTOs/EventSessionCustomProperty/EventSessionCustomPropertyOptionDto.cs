// ABOUTME: Read-only DTO for event session runtime custom property options, nested within definition DTOs.
// ABOUTME: Includes SourceTemplateOptionId for provenance tracking from template instantiation.

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public class EventSessionCustomPropertyOptionDto
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
    public Guid? SourceTemplateOptionId { get; set; }
}
