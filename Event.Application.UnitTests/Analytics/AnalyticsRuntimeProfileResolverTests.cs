// ABOUTME: Tests the core consent policy engine (AnalyticsRuntimeProfileResolver).
// ABOUTME: Covers global kill switch, provider capabilities, PostHog modes, consent computation.

using Explore.Application.Analytics;
using Explore.Application.Settings.Groups;
using Explore.Domain.Enums.Analytics;

namespace Event.Application.UnitTests.Analytics;

public class AnalyticsRuntimeProfileResolverTests
{
    private readonly AnalyticsRuntimeProfileResolver _resolver = new();

    private static AnalyticsSettingGroup CreateSettings(Action<AnalyticsSettingGroup>? configure = null)
    {
        var group = new AnalyticsSettingGroup();
        configure?.Invoke(group);
        return group;
    }

    private static AnalyticsSettingGroup CreatePosthogSettings(
        PosthogCookielessMode cookielessMode = PosthogCookielessMode.Off,
        bool cookieBannerEnabled = true,
        DeclineBehavior declineBehavior = DeclineBehavior.Disable,
        string? tenantSlug = "test-tenant")
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"posthog\"" },
            ["analytics.enabled"] = new() { Value = "true" },
            ["analytics.cookie_consent_enabled"] = new() { Value = cookieBannerEnabled.ToString().ToLower() },
            ["analytics.decline_behavior"] = new() { Value = $"\"{ToSnakeCase(declineBehavior.ToString())}\"" },
            ["analytics.posthog_cookieless_mode"] = new() { Value = $"\"{ToSnakeCase(cookielessMode.ToString())}\"" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);
        group.TenantSlug = tenantSlug;
        return group;
    }

    private static string ToSnakeCase(string value)
    {
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }

    // --- Global Kill Switch ---

    [Test]
    public async Task Resolve_WhenGlobalKillSwitchEnabled_DisablesEverything()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"posthog\"" },
            ["analytics.enabled"] = new() { Value = "true" },
            ["analytics.global_disable_client_tracking"] = new() { Value = "true" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsFalse();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.Cookieless);
        await Assert.That(result.DeclineBehavior).IsEqualTo(DeclineBehavior.Disable);
        await Assert.That(result.Posthog).IsNull();
    }

    // --- Analytics Disabled ---

    [Test]
    public async Task Resolve_WhenAnalyticsDisabled_NoBannerNoTracking()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"posthog\"" },
            ["analytics.enabled"] = new() { Value = "false" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsFalse();
    }

    [Test]
    public async Task Resolve_WhenProviderNone_NoBannerNoTracking()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"none\"" },
            ["analytics.enabled"] = new() { Value = "true" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsFalse();
    }

    // --- Inherently Cookieless Providers ---

    [Test]
    public async Task Resolve_Plausible_NoBannerCanRunBeforeConsent()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"plausible\"" },
            ["analytics.enabled"] = new() { Value = "true" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsTrue();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.Cookieless);
    }

    [Test]
    public async Task Resolve_Rybbit_NoBannerCanRunBeforeConsent()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"rybbit\"" },
            ["analytics.enabled"] = new() { Value = "true" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsTrue();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.Cookieless);
    }

    // --- PostHog Cookieless Mode Always ---

    [Test]
    public async Task Resolve_PosthogCookielessAlways_NoBannerCanRunBeforeConsent()
    {
        var settings = CreatePosthogSettings(cookielessMode: PosthogCookielessMode.Always);

        var result = _resolver.Resolve(settings);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsTrue();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.Cookieless);
        await Assert.That(result.Posthog).IsNotNull();
        await Assert.That(result.Posthog!.CookielessMode).IsEqualTo(PosthogCookielessMode.Always);
    }

    // --- PostHog Cookieless Mode OnReject ---

    [Test]
    public async Task Resolve_PosthogCookielessOnReject_ConsentManagedWithBanner()
    {
        var settings = CreatePosthogSettings(
            cookielessMode: PosthogCookielessMode.OnReject,
            cookieBannerEnabled: true,
            declineBehavior: DeclineBehavior.Cookieless);

        var result = _resolver.Resolve(settings);

        await Assert.That(result.CookieBannerEnabled).IsTrue();
        await Assert.That(result.CanRunBeforeConsent).IsTrue();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.ConsentManaged);
        await Assert.That(result.DeclineBehavior).IsEqualTo(DeclineBehavior.Cookieless);
    }

    [Test]
    public async Task Resolve_PosthogCookielessOnReject_BannerDisabled_NoBanner()
    {
        var settings = CreatePosthogSettings(
            cookielessMode: PosthogCookielessMode.OnReject,
            cookieBannerEnabled: false);

        var result = _resolver.Resolve(settings);

        await Assert.That(result.CookieBannerEnabled).IsFalse();
        await Assert.That(result.CanRunBeforeConsent).IsTrue();
    }

    // --- PostHog Cookieless Mode Off (full consent) ---

    [Test]
    public async Task Resolve_PosthogCookielessOff_FullConsentRequired()
    {
        var settings = CreatePosthogSettings(
            cookielessMode: PosthogCookielessMode.Off,
            cookieBannerEnabled: true);

        var result = _resolver.Resolve(settings);

        await Assert.That(result.CookieBannerEnabled).IsTrue();
        await Assert.That(result.CanRunBeforeConsent).IsFalse();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.FullConsent);
        await Assert.That(result.DeclineBehavior).IsEqualTo(DeclineBehavior.Disable);
    }

    // --- RudderStack (v1 full consent) ---

    [Test]
    public async Task Resolve_RudderStack_FullConsentRequired()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"rudderstack\"" },
            ["analytics.enabled"] = new() { Value = "true" },
            ["analytics.cookie_consent_enabled"] = new() { Value = "true" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.CookieBannerEnabled).IsTrue();
        await Assert.That(result.CanRunBeforeConsent).IsFalse();
        await Assert.That(result.StorageProfile).IsEqualTo(AnalyticsStorageProfile.FullConsent);
    }

    // --- Consent Cookie Key ---

    [Test]
    public async Task Resolve_SetsConsentCookieKeyFromTenantSlug()
    {
        var settings = CreatePosthogSettings(tenantSlug: "my-org");

        var result = _resolver.Resolve(settings);

        await Assert.That(result.ConsentCookieKey).IsEqualTo("explore_cc_my-org");
    }

    [Test]
    public async Task Resolve_NullTenantSlug_DefaultsCookieKey()
    {
        var settings = CreatePosthogSettings(tenantSlug: null);

        var result = _resolver.Resolve(settings);

        await Assert.That(result.ConsentCookieKey).IsEqualTo("explore_cc_default");
    }

    // --- Consent Cookie Lifetime ---

    [Test]
    public async Task Resolve_UsesConfiguredConsentLifetime()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"posthog\"" },
            ["analytics.enabled"] = new() { Value = "true" },
            ["analytics.consent_cookie_lifetime_days"] = new() { Value = "90" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.ConsentCookieLifetimeDays).IsEqualTo(90);
    }

    // --- PostHog Options ---

    [Test]
    public async Task Resolve_PosthogCookielessOff_IncludesPosthogOptions()
    {
        var settings = CreatePosthogSettings(cookielessMode: PosthogCookielessMode.Off);

        var result = _resolver.Resolve(settings);

        await Assert.That(result.Posthog).IsNotNull();
        await Assert.That(result.Posthog!.CookielessMode).IsEqualTo(PosthogCookielessMode.Off);
    }

    [Test]
    public async Task Resolve_NonPosthogProvider_NoPosthogOptions()
    {
        var settings = new Dictionary<string, Explore.Application.Contracts.Infrastructure.ResolvedSetting>
        {
            ["analytics.provider"] = new() { Value = "\"plausible\"" },
            ["analytics.enabled"] = new() { Value = "true" }
        };

        var group = new AnalyticsSettingGroup();
        group.Populate(settings);

        var result = _resolver.Resolve(group);

        await Assert.That(result.Posthog).IsNull();
    }
}
