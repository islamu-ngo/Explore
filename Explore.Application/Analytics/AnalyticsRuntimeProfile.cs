// ABOUTME: Computed runtime profile — the single source of consent/analytics policy truth.
// ABOUTME: Produced by IAnalyticsRuntimeProfileResolver; consumed by query handlers and admin UI.

namespace Explore.Application.Analytics;

using Explore.Domain.Enums.Analytics;

/// <summary>
/// The effective analytics runtime profile after applying all governance rules,
/// provider capabilities, and operator overrides.
/// </summary>
public sealed record AnalyticsRuntimeProfile
{
    public AnalyticsStorageProfile StorageProfile { get; init; }
    public bool CookieBannerEnabled { get; init; }
    public bool CanRunBeforeConsent { get; init; }
    public DeclineBehavior DeclineBehavior { get; init; }
    public string ConsentCookieKey { get; init; } = "explore_cc_default";
    public int ConsentCookieLifetimeDays { get; init; } = 180;
    public PosthogClientOptions? Posthog { get; init; }
}

/// <summary>
/// PostHog-specific client-side options computed from governance settings.
/// </summary>
public sealed record PosthogClientOptions
{
    public PosthogCookielessMode CookielessMode { get; init; }
    public PosthogPersonProfiles PersonProfiles { get; init; }
    public bool SessionReplay { get; init; }
    public bool Autocapture { get; init; }
    public bool Heatmaps { get; init; }
    public bool Toolbar { get; init; }
}
