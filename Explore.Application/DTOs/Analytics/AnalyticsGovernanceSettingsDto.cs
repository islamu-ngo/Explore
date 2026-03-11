// ABOUTME: DTO for analytics governance settings exposed via admin API.
// ABOUTME: Maps 1:1 to AnalyticsSettingGroup fields for admin read/write.

using Explore.Domain.Enums.Analytics;

namespace Explore.Application.DTOs.Analytics;

public sealed class AnalyticsGovernanceSettingsDto
{
    // Provider & basic config (read-only context from environment/secrets)
    public string Provider { get; set; } = "none";
    public bool Enabled { get; set; }
    public string? EndpointUrl { get; set; }
    public bool HasApiKey { get; set; }

    // Cookie consent & storage governance (admin-editable)
    public bool CookieConsentEnabled { get; set; }
    public DeclineBehavior DeclineBehavior { get; set; } = DeclineBehavior.Cookieless;
    public int ConsentCookieLifetimeDays { get; set; } = 180;
    public bool GlobalDisableClientTracking { get; set; }

    // PostHog privacy & feature controls (admin-editable)
    public PosthogCookielessMode PosthogCookielessMode { get; set; } = PosthogCookielessMode.OnReject;
    public PosthogPersonProfiles PosthogPersonProfiles { get; set; } = PosthogPersonProfiles.IdentifiedOnly;
    public bool PosthogSessionReplay { get; set; }
    public bool PosthogAutocapture { get; set; }
    public bool PosthogHeatmaps { get; set; }
    public bool PosthogToolbar { get; set; }

    // Computed advisory info (read-only, computed by resolver)
    public bool CookieBannerRequired { get; set; }
    public bool CanRunBeforeConsent { get; set; }
    public string StorageProfile { get; set; } = "Unknown";
}
