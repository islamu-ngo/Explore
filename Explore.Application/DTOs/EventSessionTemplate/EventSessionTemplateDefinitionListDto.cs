// ABOUTME: Lightweight list DTO for session template property definitions.
// ABOUTME: Includes OptionCount instead of full options to reduce payload size.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventSessionTemplate;

public class EventSessionTemplateDefinitionListDto
{
    public Guid Id { get; set; }
    public Guid EventSessionTemplateId { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public ExposureLevel ExposureLevel { get; set; }
    public int OptionCount { get; set; }
}
