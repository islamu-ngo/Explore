// ABOUTME: Enumerates how browser analytics traffic reaches the configured analytics provider.
// ABOUTME: Distinguishes direct vendor loading, first-party proxying, and server relay fallback.

namespace Explore.Application.Analytics;

public enum AnalyticsTransportMode
{
    Direct = 0,
    Proxy = 1,
    Relay = 2
}
