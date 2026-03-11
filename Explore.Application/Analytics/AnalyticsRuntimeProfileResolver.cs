// ABOUTME: Core consent policy engine — computes effective analytics runtime behavior.
// ABOUTME: Checks global kill switch, provider capabilities, storage profile, and PostHog options.

namespace Explore.Application.Analytics;

using Explore.Application.Contracts.Services;
using Explore.Application.Settings.Groups;
using Explore.Domain.Analytics;
using Explore.Domain.Enums;
using Explore.Domain.Enums.Analytics;

public sealed class AnalyticsRuntimeProfileResolver : IAnalyticsRuntimeProfileResolver
{
    public AnalyticsRuntimeProfile Resolve(AnalyticsSettingGroup settings)
    {
        var consentCookieKey = $"explore_cc_{settings.TenantSlug ?? "default"}";
        var consentLifetime = settings.ConsentCookieLifetimeDays;

        // Global kill switch — disables all browser analytics immediately
        if (settings.GlobalDisableClientTracking)
        {
            return new AnalyticsRuntimeProfile
            {
                StorageProfile = AnalyticsStorageProfile.Cookieless,
                CookieBannerEnabled = false,
                CanRunBeforeConsent = false,
                DeclineBehavior = DeclineBehavior.Disable,
                ConsentCookieKey = consentCookieKey,
                ConsentCookieLifetimeDays = consentLifetime
            };
        }

        // Analytics disabled — no banner, no tracking
        if (!settings.Enabled || settings.ProviderEnum == AnalyticsProviderEnum.None)
        {
            return new AnalyticsRuntimeProfile
            {
                StorageProfile = AnalyticsStorageProfile.Cookieless,
                CookieBannerEnabled = false,
                CanRunBeforeConsent = false,
                DeclineBehavior = DeclineBehavior.Disable,
                ConsentCookieKey = consentCookieKey,
                ConsentCookieLifetimeDays = consentLifetime
            };
        }

        var capabilities = AnalyticsProviderCapabilities.For(settings.ProviderEnum);

        // Inherently cookieless providers (Plausible, Rybbit, None) — no banner needed
        if (capabilities.InherentlyCookieless)
        {
            return new AnalyticsRuntimeProfile
            {
                StorageProfile = AnalyticsStorageProfile.Cookieless,
                CookieBannerEnabled = false,
                CanRunBeforeConsent = true,
                DeclineBehavior = DeclineBehavior.Disable,
                ConsentCookieKey = consentCookieKey,
                ConsentCookieLifetimeDays = consentLifetime
            };
        }

        // PostHog — storage profile depends on cookieless mode
        if (settings.ProviderEnum == AnalyticsProviderEnum.Posthog)
        {
            return ResolvePosthog(settings, consentCookieKey, consentLifetime);
        }

        // RudderStack and other providers — full consent required (Amendment 7)
        return new AnalyticsRuntimeProfile
        {
            StorageProfile = AnalyticsStorageProfile.FullConsent,
            CookieBannerEnabled = settings.CookieConsentEnabled,
            CanRunBeforeConsent = false,
            DeclineBehavior = DeclineBehavior.Disable,
            ConsentCookieKey = consentCookieKey,
            ConsentCookieLifetimeDays = consentLifetime
        };
    }

    private static AnalyticsRuntimeProfile ResolvePosthog(
        AnalyticsSettingGroup settings,
        string consentCookieKey,
        int consentLifetime)
    {
        var posthogOptions = new PosthogClientOptions
        {
            CookielessMode = settings.PosthogCookielessMode,
            PersonProfiles = settings.PosthogPersonProfiles,
            SessionReplay = settings.PosthogSessionReplay,
            Autocapture = settings.PosthogAutocapture,
            Heatmaps = settings.PosthogHeatmaps,
            Toolbar = settings.PosthogToolbar
        };

        return settings.PosthogCookielessMode switch
        {
            PosthogCookielessMode.Always => new AnalyticsRuntimeProfile
            {
                StorageProfile = AnalyticsStorageProfile.Cookieless,
                CookieBannerEnabled = false,
                CanRunBeforeConsent = true,
                DeclineBehavior = DeclineBehavior.Disable,
                ConsentCookieKey = consentCookieKey,
                ConsentCookieLifetimeDays = consentLifetime,
                Posthog = posthogOptions
            },
            PosthogCookielessMode.OnReject => new AnalyticsRuntimeProfile
            {
                StorageProfile = AnalyticsStorageProfile.ConsentManaged,
                CookieBannerEnabled = settings.CookieConsentEnabled,
                CanRunBeforeConsent = true,
                DeclineBehavior = settings.DeclineBehavior,
                ConsentCookieKey = consentCookieKey,
                ConsentCookieLifetimeDays = consentLifetime,
                Posthog = posthogOptions
            },
            _ => new AnalyticsRuntimeProfile
            {
                StorageProfile = AnalyticsStorageProfile.FullConsent,
                CookieBannerEnabled = settings.CookieConsentEnabled,
                CanRunBeforeConsent = false,
                DeclineBehavior = DeclineBehavior.Disable,
                ConsentCookieKey = consentCookieKey,
                ConsentCookieLifetimeDays = consentLifetime,
                Posthog = posthogOptions
            }
        };
    }
}
