// ABOUTME: Aggregate-view DTO describing one session-scoped custom-property facet aggregated at event scope.
// ABOUTME: Mirrors the event facet shape while remaining explicit for session-derived extension data.

using System.Text.Json;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventAggregateView;

public sealed class EventSessionCustomPropertyFacetDto
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
