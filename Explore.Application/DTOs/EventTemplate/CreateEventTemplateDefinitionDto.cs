// ABOUTME: Write DTO for creating template property definitions, includes all property fields and nested options.
// ABOUTME: Used both nested within CreateEventTemplateDto and standalone for adding definitions to existing templates.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventTemplate;

public class CreateEventTemplateDefinitionDto
{
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PropertyType PropertyType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsMulti { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public ExposureLevel ExposureLevel { get; set; }

    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
    public bool IsSystemOwned { get; set; }

    public string? DefaultTextValue { get; set; }
    public decimal? DefaultNumberValue { get; set; }
    public bool? DefaultBooleanValue { get; set; }
    public DateTimeOffset? DefaultDateTimeValue { get; set; }
    public Guid? DefaultOptionId { get; set; }

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? RegexPattern { get; set; }
    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }
    public DateTimeOffset? MinDateTime { get; set; }
    public DateTimeOffset? MaxDateTime { get; set; }
    public string? AllowedUrlSchemes { get; set; }

    public List<CreateEventTemplateOptionDto> Options { get; set; } = [];
}
