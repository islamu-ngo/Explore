// ABOUTME: Defines a canonical analytics event and the property keys it may emit.
// ABOUTME: Keeps business handlers aligned with a shared taxonomy and property allowlist.

namespace Explore.Application.Analytics;

public sealed record AnalyticsEventDefinition(
    string EventName,
    IReadOnlySet<string> AllowedPropertyKeys,
    bool RequiresIdentifiedTracking = false);

public sealed record SanitizedAnalyticsTrackRequest(
    string DistinctId,
    string EventName,
    IReadOnlyDictionary<string, object> Properties);

public sealed record SanitizedAnalyticsPageViewRequest(
    string DistinctId,
    string PagePath,
    IReadOnlyDictionary<string, object> Properties);
