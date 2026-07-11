// ABOUTME: Aggregate-view DTO describing one event-scoped custom-property facet and its values.
// ABOUTME: Uses JsonElement values so typed projection data survives without reflection-based unions.

using System.Text.Json;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventAggregateView;

public sealed class EventCustomPropertyFacetDto
{
    public string Namespace { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public ExposureLevel ExposureLevel { get; set; }
    public IReadOnlyList<JsonElement> Values { get; set; } = [];
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsExportable { get; set; }
    public bool IsModerationRelevant { get; set; }
    public bool IsAnalyticsRelevant { get; set; }
}
