// ABOUTME: Unit tests for lifecycle policy provider default and tenant-aware composition.
// ABOUTME: Verifies hard invariant profiles stay central and tenant lookup is invoked only when scoped.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Services.Federation;
using Explore.Application.Services.Lifecycle;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public sealed class EventLifecyclePolicyProviderTests
{
    private readonly ITenantPolicySettingService _tenantPolicySettingService = Substitute.For<ITenantPolicySettingService>();

    [Test]
    public async Task GetEffectivePolicyAsync_WhenTenantIsNull_ReturnsDefaultPublishPolicyWithoutTenantLookup()
    {
        var provider = CreateProvider();

        var policy = await provider.GetEffectivePolicyAsync(null, ValidationProfile.EventPublish, CancellationToken.None);

        await Assert.That(policy.Profile).IsEqualTo(ValidationProfile.EventPublish);
        await Assert.That(policy.Source).IsEqualTo("default");
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.Title);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.ScheduleSessions);
        await Assert.That(policy.RequiredSessionFields).IsEmpty();
        await _tenantPolicySettingService.DidNotReceive().ReadEffectiveTenantSettingsAsync(Arg.Any<Guid>());
    }

    [Test]
    public async Task GetEffectivePolicyAsync_WhenCommunityProfileIsRequestedWithoutTenant_FallsBackToPlatformPublish()
    {
        var provider = CreateProvider();

        EventLifecyclePolicy policy = await provider.GetEffectivePolicyAsync(
            null,
            ValidationProfile.EventPublishCommunityLexicon,
            CancellationToken.None);

        await Assert.That(policy.Profile).IsEqualTo(ValidationProfile.EventPublish);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.ScheduleSessions);
        await _tenantPolicySettingService.DidNotReceive().ReadEffectiveTenantSettingsAsync(Arg.Any<Guid>());
    }

    [Test]
    public async Task GetEffectivePolicyAsync_WhenTenantIsPresent_ReturnsTenantAwarePolicyAndPreservesHardInvariants()
    {
        var tenantId = Guid.NewGuid();
        _tenantPolicySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        var provider = CreateProvider();

        var policy = await provider.GetEffectivePolicyAsync(tenantId, ValidationProfile.SessionPublish, CancellationToken.None);

        await Assert.That(policy.Profile).IsEqualTo(ValidationProfile.SessionPublish);
        await Assert.That(policy.Source).IsEqualTo("tenant-aware");
        await Assert.That(policy.RequiredSessionFields).Contains(EventSessionFieldKey.ParentEventCompatibility);
        await Assert.That(policy.RequiredSessionFields).Contains(EventSessionFieldKey.ScheduleStart);
        await Assert.That(policy.RequiredEventFields).IsEmpty();
        await _tenantPolicySettingService.Received(1).ReadEffectiveTenantSettingsAsync(tenantId);
    }

    [Test]
    [Arguments(false, "community_lexicon", ValidationProfile.EventPublish)]
    [Arguments(true, "platform", ValidationProfile.EventPublish)]
    [Arguments(true, "unknown", ValidationProfile.EventPublish)]
    [Arguments(true, "community_lexicon", ValidationProfile.EventPublishCommunityLexicon)]
    public async Task GetEffectivePolicyAsync_EventPublishSelectsOnlyEnabledKnownCommunityProfile(
        bool eventsEnabled,
        string storedProfile,
        ValidationProfile expectedProfile)
    {
        var tenantId = Guid.NewGuid();
        _tenantPolicySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        settingsResolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<IEnumerable<string>>(0).Select(key => new ResolvedSetting
            {
                Key = key,
                Value = key == GovernanceSettingKeys.Federation.AtprotoEventsEnabled
                    ? eventsEnabled ? "true" : "false"
                    : $"\"{storedProfile}\"",
                ValueType = key == GovernanceSettingKeys.Federation.AtprotoEventsEnabled
                    ? SettingValueType.Boolean
                    : SettingValueType.String,
                Source = SettingSource.SystemDefault
            }).ToArray());
        var provider = CreateProvider(settingsResolver);

        EventLifecyclePolicy policy = await provider.GetEffectivePolicyAsync(
            tenantId,
            ValidationProfile.EventPublish,
            CancellationToken.None);

        await Assert.That(policy.Profile).IsEqualTo(expectedProfile);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.Title);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.Tenant);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.Owner);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.Status);
        await Assert.That(policy.RequiredEventFields.Contains(EventFieldKey.ScheduleSessions))
            .IsEqualTo(expectedProfile == ValidationProfile.EventPublish);
    }

    private EventLifecyclePolicyProvider CreateProvider(IHierarchicalSettingsResolver? settingsResolver = null)
    {
        settingsResolver ??= Substitute.For<IHierarchicalSettingsResolver>();
        return new EventLifecyclePolicyProvider(
            _tenantPolicySettingService,
            new AtprotoEventGovernanceResolver(settingsResolver));
    }
}
