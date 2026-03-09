// ABOUTME: Browser-to-server analytics relay payload for relay transport mode.
// ABOUTME: Keeps the public payload narrow so the server can re-apply tenant-aware governance rules.

using System.Text.Json;

namespace Explore.Application.DTOs.Analytics;

public class RelayAnalyticsEventDto
{
    public string EventType { get; set; } = "pageview";
    public string EventName { get; set; } = string.Empty;
    public string PagePath { get; set; } = string.Empty;
    public string DistinctId { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, JsonElement> Traits { get; set; } = new(StringComparer.Ordinal);
}
