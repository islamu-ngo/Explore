// ABOUTME: PostHog SDK cookieless_mode configuration.
// ABOUTME: Off = standard cookies/localStorage; Always = never stores on device; OnReject = cookieless after decline.

namespace Explore.Domain.Enums.Analytics;

public enum PosthogCookielessMode
{
    Off = 0,
    Always = 1,
    OnReject = 2
}
