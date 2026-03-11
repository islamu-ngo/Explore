// ABOUTME: Client-side model for admin analytics & privacy settings form.
// ABOUTME: Mirrors AnalyticsGovernanceSettingsDto for two-way binding in Blazor admin UI.

namespace Explore.Blazor.Client.Models.Analytics;

public sealed class AnalyticsGovernanceSettingsModel
{
    // Provider info (read-only in UI, from environment/secrets)
    public string Provider { get; set; } = "none";
    public bool Enabled { get; set; }
    public string? EndpointUrl { get; set; }
    public bool HasApiKey { get; set; }

    // Cookie consent & storage governance (admin-editable)
    public bool CookieConsentEnabled { get; set; }
    public string DeclineBehavior { get; set; } = "Cookieless";
    public int ConsentCookieLifetimeDays { get; set; } = 180;
    public bool GlobalDisableClientTracking { get; set; }

    // PostHog privacy & feature controls (admin-editable)
    public string PosthogCookielessMode { get; set; } = "OnReject";
    public string PosthogPersonProfiles { get; set; } = "IdentifiedOnly";
    public bool PosthogSessionReplay { get; set; }
    public bool PosthogAutocapture { get; set; }
    public bool PosthogHeatmaps { get; set; }
    public bool PosthogToolbar { get; set; }

    // Computed advisory (read-only, from resolver)
    public bool CookieBannerRequired { get; set; }
    public bool CanRunBeforeConsent { get; set; }
    public string StorageProfile { get; set; } = "Unknown";

    public bool IsPosthog => string.Equals(Provider, "posthog", StringComparison.OrdinalIgnoreCase);
    public bool IsRybbit => string.Equals(Provider, "rybbit", StringComparison.OrdinalIgnoreCase);
    public bool HasProvider => !string.Equals(Provider, "none", StringComparison.OrdinalIgnoreCase) && Enabled;
}
