// ABOUTME: Unit tests for lifecycle policy provider default and tenant-aware composition.
// ABOUTME: Verifies hard invariant profiles stay central and tenant lookup is invoked only when scoped.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Services.Lifecycle;
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
        var provider = new EventLifecyclePolicyProvider(_tenantPolicySettingService);

        var policy = await provider.GetEffectivePolicyAsync(null, ValidationProfile.EventPublish, CancellationToken.None);

        await Assert.That(policy.Profile).IsEqualTo(ValidationProfile.EventPublish);
        await Assert.That(policy.Source).IsEqualTo("default");
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.Title);
        await Assert.That(policy.RequiredEventFields).Contains(EventFieldKey.ScheduleSessions);
        await Assert.That(policy.RequiredSessionFields).IsEmpty();
        await _tenantPolicySettingService.DidNotReceive().ReadEffectiveTenantSettingsAsync(Arg.Any<Guid>());
    }

    [Test]
    public async Task GetEffectivePolicyAsync_WhenTenantIsPresent_ReturnsTenantAwarePolicyAndPreservesHardInvariants()
    {
        var tenantId = Guid.NewGuid();
        _tenantPolicySettingService.ReadEffectiveTenantSettingsAsync(tenantId).Returns(new TenantPolicySettingsDto());
        var provider = new EventLifecyclePolicyProvider(_tenantPolicySettingService);

        var policy = await provider.GetEffectivePolicyAsync(tenantId, ValidationProfile.SessionPublish, CancellationToken.None);

        await Assert.That(policy.Profile).IsEqualTo(ValidationProfile.SessionPublish);
        await Assert.That(policy.Source).IsEqualTo("tenant-aware");
        await Assert.That(policy.RequiredSessionFields).Contains(EventSessionFieldKey.ParentEventCompatibility);
        await Assert.That(policy.RequiredSessionFields).Contains(EventSessionFieldKey.ScheduleStart);
        await Assert.That(policy.RequiredEventFields).IsEmpty();
        await _tenantPolicySettingService.Received(1).ReadEffectiveTenantSettingsAsync(tenantId);
    }
}
