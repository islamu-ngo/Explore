// ABOUTME: Slim public DTO for browser analytics bootstrap — effective runtime config only.
// ABOUTME: No admin governance inputs, no tenantSlug, no PersonalApiKey. Amendment 3 compliant.

namespace Explore.Application.DTOs.Onboarding;

/// <summary>
/// Public bootstrap payload for browser-side analytics consent and initialization.
/// Contains only computed, effective runtime configuration — no admin-facing governance inputs.
/// </summary>
public sealed class AnalyticsConsentBootstrapDto
{
    // Consent UX
    public bool CookieBannerEnabled { get; set; }
    public bool CanRunBeforeConsent { get; set; }
    public string DeclineBehavior { get; set; } = "disable";
    public string ConsentCookieKey { get; set; } = "explore_cc_default";
    public int ConsentCookieLifetimeDays { get; set; } = 180;

    // Provider runtime config (public keys only)
    public string AnalyticsProvider { get; set; } = "none";
    public PosthogClientBootstrapDto? Posthog { get; set; }
}

/// <summary>
/// PostHog-specific client bootstrap options.
/// Enum values are mapped to JS string literals at this DTO boundary.
/// </summary>
public sealed class PosthogClientBootstrapDto
{
    public string CookielessMode { get; set; } = "off";
    public string PersonProfiles { get; set; } = "identified_only";
    public bool SessionReplay { get; set; }
    public bool Autocapture { get; set; }
    public bool Heatmaps { get; set; }
    public bool Toolbar { get; set; }
}
