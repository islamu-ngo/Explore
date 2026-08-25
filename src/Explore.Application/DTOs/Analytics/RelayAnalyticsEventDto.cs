// ABOUTME: Browser-to-server analytics relay payload for relay transport mode.
// ABOUTME: Keeps the public payload narrow so the server can re-apply tenant-aware governance rules.

using System.Text.Json;

namespace Explore.Application.DTOs.Analytics;

public sealed record RelayAnalyticsEventDto
{
    public string EventType { get; init; } = "pageview";
    public string EventName { get; init; } = string.Empty;
    public string PagePath { get; init; } = string.Empty;
    public string DistinctId { get; init; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, JsonElement> Traits { get; init; } = new(StringComparer.Ordinal);
}
