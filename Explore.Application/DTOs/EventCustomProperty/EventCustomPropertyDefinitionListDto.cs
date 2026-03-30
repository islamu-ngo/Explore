// ABOUTME: Lightweight list DTO for event runtime custom property definitions.
// ABOUTME: Includes OptionCount and SourceTemplateId for quick provenance visibility.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventCustomProperty;

public class EventCustomPropertyDefinitionListDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
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
