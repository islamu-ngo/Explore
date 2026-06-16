// ABOUTME: Client-side detail model mirroring Explore.Application's CustomPropertyDefinitionDto.
// ABOUTME: Used when the admin UI needs the full definition payload for editing flag state.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Models.CustomProperties;

/// <summary>
/// Full details of a shared Layer 3 custom-property definition.
/// </summary>
public sealed class CustomPropertyDefinitionDetailModel
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public EntityTypeName EntityTypeName { get; set; }
    public Guid TenantId { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
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
    public List<CustomPropertyOptionModel> Options { get; set; } = [];

    [System.Text.Json.Serialization.JsonPropertyName("_links")]
    public Dictionary<string, object>? Links { get; set; } = new();
}
