// ABOUTME: Write DTO for creating template property definitions, includes all property fields and nested options.
// ABOUTME: Used both nested within CreateEventTemplateDto and standalone for adding definitions to existing templates.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventTemplate;

public record CreateEventTemplateDefinitionDto
{
    public string Namespace { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public PropertyType PropertyType { get; init; }
    public bool IsRequired { get; init; }
    public bool IsMulti { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public ExposureLevel ExposureLevel { get; init; }

    public bool IsSearchable { get; init; }
    public bool IsFilterable { get; init; }
    public bool IsExportable { get; init; }
    public bool IsModerationRelevant { get; init; }
    public bool IsAnalyticsRelevant { get; init; }
    public bool IsSystemOwned { get; init; }

    public string? DefaultTextValue { get; init; }
    public decimal? DefaultNumberValue { get; init; }
    public bool? DefaultBooleanValue { get; init; }
    public DateTimeOffset? DefaultDateTimeValue { get; init; }
    public Guid? DefaultOptionId { get; init; }

    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? RegexPattern { get; init; }
    public decimal? MinNumber { get; init; }
    public decimal? MaxNumber { get; init; }
    public DateTimeOffset? MinDateTime { get; init; }
    public DateTimeOffset? MaxDateTime { get; init; }
    public string? AllowedUrlSchemes { get; init; }

    public List<CreateEventTemplateOptionDto> Options { get; init; } = [];
}
