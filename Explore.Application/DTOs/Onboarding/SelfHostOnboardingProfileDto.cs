// ABOUTME: Application-owned first-run profile for convention-first self-hosted onboarding.
// ABOUTME: Captures public site identity without introducing domain workspace or tenant-scope concepts.

namespace Explore.Application.DTOs.Onboarding;

public sealed class SelfHostOnboardingProfileDto
{
    public string SiteName { get; set; } = string.Empty;
    public string? SupportEmail { get; set; }
    public string? CanonicalUrl { get; set; }
    public string Locale { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public string? Purpose { get; set; }
}
