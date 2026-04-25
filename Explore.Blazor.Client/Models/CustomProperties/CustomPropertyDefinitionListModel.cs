// ABOUTME: Client-side lightweight read model mirroring Explore.Application's CustomPropertyDefinitionListDto.
// ABOUTME: Deserialized from HAL payloads via JSON round-trip since NSwag generates the payload as ICollection<object>.

using Explore.Domain.Enums;

namespace Explore.Blazor.Client.Models.CustomProperties;

/// <summary>
/// Governance list row for a shared Layer 3 custom-property definition.
/// </summary>
/// <remarks>
/// Mirrors the server DTO 1:1. Populated by <c>HalResourceExtensions.GetItems()</c> which
/// performs a JSON round-trip because NSwag flattens the server's typed HAL payload to
/// <c>ICollection&lt;object&gt;</c>.
/// </remarks>
public sealed class CustomPropertyDefinitionListModel
{
    public Guid Id { get; set; }
    public EntityTypeName EntityTypeName { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
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

    [System.Text.Json.Serialization.JsonPropertyName("_links")]
    public Dictionary<string, object>? Links { get; set; } = new();
}
