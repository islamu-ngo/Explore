// ABOUTME: Lightweight read DTO for paginated shared Layer 3 custom-property definition lists.
// ABOUTME: Keeps admin list screens efficient while still exposing machine identity and governance flags.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.CustomPropertyDefinition;

public class CustomPropertyDefinitionListDto
{
    public Guid Id { get; set; }
    public EntityTypeName EntityTypeName { get; set; }
    public required string Namespace { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public PropertyType PropertyType { get; set; }
    public bool IsRequired { get; set; }
    public bool IsMulti { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public ExposureLevel ExposureLevel { get; set; }
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
    public bool IsSystemOwned { get; set; }
    public int OptionCount { get; set; }
}
