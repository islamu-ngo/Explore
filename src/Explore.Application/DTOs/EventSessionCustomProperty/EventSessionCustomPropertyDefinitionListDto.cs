// ABOUTME: Lightweight list DTO for event session runtime custom property definitions.
// ABOUTME: Includes OptionCount and SourceTemplateId for quick provenance visibility.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventSessionCustomProperty;

public class EventSessionCustomPropertyDefinitionListDto
{
    public Guid Id { get; set; }
    public Guid EventSessionId { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public ExposureLevel ExposureLevel { get; set; }
    public Guid? SourceTemplateId { get; set; }
    public int OptionCount { get; set; }
}
