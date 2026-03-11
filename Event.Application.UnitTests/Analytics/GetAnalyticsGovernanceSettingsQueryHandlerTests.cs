// ABOUTME: Tests GetAnalyticsGovernanceSettingsQueryHandler DTO mapping from settings group and runtime profile.
// ABOUTME: Verifies correct delegation to IHierarchicalSettingsResolver and IAnalyticsRuntimeProfileResolver.

using Explore.Application.Analytics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Analytics;
using Explore.Application.Features.InstanceOnboarding.Handlers.Queries;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Enums.Analytics;
using NSubstitute;

namespace Event.Application.UnitTests.Analytics;

public class GetAnalyticsGovernanceSettingsQueryHandlerTests
{
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IAnalyticsRuntimeProfileResolver _runtimeProfileResolver;
    private readonly GetAnalyticsGovernanceSettingsQueryHandler _handler;

    public GetAnalyticsGovernanceSettingsQueryHandlerTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _runtimeProfileResolver = Substitute.For<IAnalyticsRuntimeProfileResolver>();

        _settingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AnalyticsSettingGroup());

        _runtimeProfileResolver.Resolve(Arg.Any<AnalyticsSettingGroup>())
            .Returns(new AnalyticsRuntimeProfile());

        _handler = new GetAnalyticsGovernanceSettingsQueryHandler(
            _settingsResolver,
            _runtimeProfileResolver);
    }

    private static AnalyticsSettingGroup CreatePopulatedGroup(
        params (string key, string value)[] entries)
    {
        var dict = entries.ToDictionary(e => e.key, e => new ResolvedSetting { Value = e.value });
        var group = new AnalyticsSettingGroup();
        group.Populate(dict);
        return group;
    }

    // --- Default settings mapping ---

    [Test]
    public async Task Handle_DefaultSettings_ReturnsPrivacyFirstDefaults()
    {
        var result = await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.Provider).IsEqualTo("none");
        await Assert.That(result.Enabled).IsFalse();
        await Assert.That(result.HasApiKey).IsFalse();
        await Assert.That(result.CookieConsentEnabled).IsFalse();
        await Assert.That(result.GlobalDisableClientTracking).IsFalse();
        await Assert.That(result.PosthogSessionReplay).IsFalse();
        await Assert.That(result.PosthogAutocapture).IsFalse();
        await Assert.That(result.PosthogHeatmaps).IsFalse();
        await Assert.That(result.PosthogToolbar).IsFalse();
    }

    // --- Full settings mapping ---

    [Test]
    public async Task Handle_WithPosthogSettings_MapsAllGroupFieldsToDto()
    {
        var group = CreatePopulatedGroup(
            ("analytics.provider", "\"posthog\""),
            ("analytics.enabled", "true"),
            ("analytics.endpoint_url", "\"https://ph.example.com\""),
            ("analytics.api_key", "\"phc_test123\""),
            ("analytics.cookie_consent_enabled", "true"),
            ("analytics.decline_behavior", "\"cookieless\""),
            ("analytics.consent_cookie_lifetime_days", "90"),
            ("analytics.global_disable_client_tracking", "false"),
            ("analytics.posthog_cookieless_mode", "\"on_reject\""),
            ("analytics.posthog_person_profiles", "\"identified_only\""),
            ("analytics.posthog_session_replay", "true"),
            ("analytics.posthog_autocapture", "true"),
            ("analytics.posthog_heatmaps", "true"),
            ("analytics.posthog_toolbar", "false"));

        _settingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(group);

        var profile = new AnalyticsRuntimeProfile
        {
            CookieBannerEnabled = true,
            CanRunBeforeConsent = true,
            StorageProfile = AnalyticsStorageProfile.ConsentManaged
        };
        _runtimeProfileResolver.Resolve(group).Returns(profile);

        var result = await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.Provider).IsEqualTo("posthog");
        await Assert.That(result.Enabled).IsTrue();
        await Assert.That(result.EndpointUrl).IsEqualTo("https://ph.example.com");
        await Assert.That(result.HasApiKey).IsTrue();
        await Assert.That(result.CookieConsentEnabled).IsTrue();
        await Assert.That(result.DeclineBehavior).IsEqualTo(DeclineBehavior.Cookieless);
        await Assert.That(result.ConsentCookieLifetimeDays).IsEqualTo(90);
        await Assert.That(result.GlobalDisableClientTracking).IsFalse();
        await Assert.That(result.PosthogCookielessMode).IsEqualTo(PosthogCookielessMode.OnReject);
        await Assert.That(result.PosthogPersonProfiles).IsEqualTo(PosthogPersonProfiles.IdentifiedOnly);
        await Assert.That(result.PosthogSessionReplay).IsTrue();
        await Assert.That(result.PosthogAutocapture).IsTrue();
        await Assert.That(result.PosthogHeatmaps).IsTrue();
        await Assert.That(result.PosthogToolbar).IsFalse();
    }

    // --- HasApiKey logic ---

    [Test]
    public async Task Handle_ApiKeyPresent_HasApiKeyIsTrue()
    {
        var group = CreatePopulatedGroup(("analytics.api_key", "\"phc_abc\""));

        _settingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(group);

        var result = await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.HasApiKey).IsTrue();
    }

    [Test]
    public async Task Handle_ApiKeyMissing_HasApiKeyIsFalse()
    {
        var result = await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.HasApiKey).IsFalse();
    }

    // --- Computed advisory fields from profile ---

    [Test]
    public async Task Handle_MapsComputedAdvisoryFieldsFromProfile()
    {
        var profile = new AnalyticsRuntimeProfile
        {
            CookieBannerEnabled = true,
            CanRunBeforeConsent = false,
            StorageProfile = AnalyticsStorageProfile.FullConsent
        };
        _runtimeProfileResolver.Resolve(Arg.Any<AnalyticsSettingGroup>()).Returns(profile);

        var result = await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.CookieBannerRequired).IsTrue();
        await Assert.That(result.CanRunBeforeConsent).IsFalse();
        await Assert.That(result.StorageProfile).IsEqualTo("FullConsent");
    }

    [Test]
    public async Task Handle_CookielessProfile_StorageProfileStringMatchesEnum()
    {
        var profile = new AnalyticsRuntimeProfile
        {
            StorageProfile = AnalyticsStorageProfile.Cookieless
        };
        _runtimeProfileResolver.Resolve(Arg.Any<AnalyticsSettingGroup>()).Returns(profile);

        var result = await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await Assert.That(result.StorageProfile).IsEqualTo("Cookieless");
    }

    // --- Resolver interaction ---

    [Test]
    public async Task Handle_ResolvesSettingsWithParameterlessSettingContext()
    {
        await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        await _settingsResolver.Received(1).ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Is<SettingContext>(ctx =>
                ctx.TenantId == null &&
                ctx.OrganizationId == null &&
                ctx.GroupId == null &&
                ctx.UserId == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_PassesResolvedGroupToProfileResolver()
    {
        var group = CreatePopulatedGroup(("analytics.provider", "\"posthog\""));

        _settingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(group);

        await _handler.Handle(
            new GetAnalyticsGovernanceSettingsQuery(), CancellationToken.None);

        _runtimeProfileResolver.Received(1).Resolve(group);
    }
}
