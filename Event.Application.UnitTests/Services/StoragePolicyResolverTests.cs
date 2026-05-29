// ABOUTME: Unit tests for effective storage policy resolution.
// ABOUTME: Verifies provider normalization, tenant delegation locks, quota values, and upload ceilings.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models.Storage;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class StoragePolicyResolverTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();

    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IFileStorageProviderResolver _providerResolver = Substitute.For<IFileStorageProviderResolver>();

    [Test]
    public async Task ResolveAsync_WithoutTenant_UsesInstancePolicy()
    {
        SetupInstanceSettings(CreateSettings(provider: StorageProviders.Local, maxUploadBytes: 20, quotaBytes: 200, instanceMaxUploadBytes: 100));
        var resolver = CreateResolver();

        var policy = await resolver.ResolveAsync(null);

        await Assert.That(policy.TenantId).IsNull();
        await Assert.That(policy.Provider).IsEqualTo(StorageProviders.Local);
        await Assert.That(policy.MaxUploadBytes).IsEqualTo(20);
        await Assert.That(policy.TenantQuotaBytes).IsEqualTo(200);
        await Assert.That(policy.InstanceMaxUploadBytes).IsEqualTo(100);
        await Assert.That(policy.TenantOverridesAllowed).IsFalse();
    }

    [Test]
    public async Task ResolveAsync_MultiTenantWithStorageLocked_IgnoresTenantOverrides()
    {
        SetupInstanceSettings(CreateSettings(
            deploymentMode: "MultiTenant",
            lockStorage: true,
            provider: StorageProviders.Local,
            maxUploadBytes: 20,
            quotaBytes: 200,
            instanceMaxUploadBytes: 100));
        SetupTenantSettings(CreateSettings(provider: StorageProviders.S3Compatible, maxUploadBytes: 90, quotaBytes: 900));
        var resolver = CreateResolver();

        var policy = await resolver.ResolveAsync(TenantId);

        await Assert.That(policy.Provider).IsEqualTo(StorageProviders.Local);
        await Assert.That(policy.MaxUploadBytes).IsEqualTo(20);
        await Assert.That(policy.TenantQuotaBytes).IsEqualTo(200);
        await Assert.That(policy.TenantOverridesAllowed).IsFalse();
        await Assert.That(policy.TenantStorageLocked).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_MultiTenantUnlocked_AppliesTenantProviderAndCapsMaxUpload()
    {
        SetupInstanceSettings(CreateSettings(
            deploymentMode: "MultiTenant",
            lockStorage: false,
            provider: StorageProviders.Local,
            maxUploadBytes: 20,
            quotaBytes: 200,
            instanceMaxUploadBytes: 50));
        SetupTenantSettings(CreateSettings(provider: StorageProviders.S3Compatible, maxUploadBytes: 90, quotaBytes: 900));
        var resolver = CreateResolver();

        var policy = await resolver.ResolveAsync(TenantId);

        await Assert.That(policy.Provider).IsEqualTo(StorageProviders.S3Compatible);
        await Assert.That(policy.MaxUploadBytes).IsEqualTo(50);
        await Assert.That(policy.TenantQuotaBytes).IsEqualTo(900);
        await Assert.That(policy.InstanceMaxUploadBytes).IsEqualTo(50);
        await Assert.That(policy.TenantOverridesAllowed).IsTrue();
        await Assert.That(policy.TenantStorageLocked).IsFalse();
    }

    [Test]
    public async Task ResolveAsync_SingleTenant_AllowsTenantOverrideEvenWhenDelegationLocked()
    {
        SetupInstanceSettings(CreateSettings(
            deploymentMode: "SingleTenant",
            lockStorage: true,
            provider: StorageProviders.Local,
            maxUploadBytes: 20,
            quotaBytes: 200,
            instanceMaxUploadBytes: 100));
        SetupTenantSettings(CreateSettings(provider: StorageProviders.S3Compatible, maxUploadBytes: 75, quotaBytes: 750));
        var resolver = CreateResolver();

        var policy = await resolver.ResolveAsync(TenantId);

        await Assert.That(policy.Provider).IsEqualTo(StorageProviders.S3Compatible);
        await Assert.That(policy.MaxUploadBytes).IsEqualTo(75);
        await Assert.That(policy.TenantQuotaBytes).IsEqualTo(750);
        await Assert.That(policy.TenantOverridesAllowed).IsTrue();
    }

    [Test]
    public async Task ResolveAsync_WithUnsupportedProvider_FallsBackToLocal()
    {
        SetupInstanceSettings(CreateSettings(provider: StorageProviders.LegacyExternal));
        var resolver = CreateResolver();

        var policy = await resolver.ResolveAsync(null);

        await Assert.That(policy.Provider).IsEqualTo(StorageProviders.Local);
    }

    [Test]
    public async Task ResolveProviderAsync_UsesEffectiveProvider()
    {
        SetupInstanceSettings(CreateSettings(deploymentMode: "MultiTenant", lockStorage: false));
        SetupTenantSettings(CreateSettings(provider: StorageProviders.S3Compatible));
        var provider = Substitute.For<IFileStorageProvider>();
        _providerResolver.GetRequired(StorageProviders.S3Compatible).Returns(provider);
        var resolver = CreateResolver();

        var result = await resolver.ResolveProviderAsync(TenantId);

        await Assert.That(result).IsSameReferenceAs(provider);
        _providerResolver.Received(1).GetRequired(StorageProviders.S3Compatible);
    }

    private StoragePolicyResolver CreateResolver()
        => new(_settingsResolver, _providerResolver);

    private void SetupInstanceSettings(IReadOnlyList<ResolvedSetting> settings)
    {
        _settingsResolver
            .ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Is<SettingContext>(context => context.TenantId == null),
                Arg.Any<CancellationToken>())
            .Returns(settings);
    }

    private void SetupTenantSettings(IReadOnlyList<ResolvedSetting> settings)
    {
        _settingsResolver
            .ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Is<SettingContext>(context => context.TenantId == TenantId),
                Arg.Any<CancellationToken>())
            .Returns(settings);
    }

    private static IReadOnlyList<ResolvedSetting> CreateSettings(
        string deploymentMode = "MultiTenant",
        bool lockStorage = true,
        string provider = StorageProviders.Local,
        long maxUploadBytes = 10 * 1024 * 1024,
        long quotaBytes = 1024L * 1024 * 1024,
        long instanceMaxUploadBytes = 100L * 1024 * 1024)
        =>
        [
            Setting(GovernanceSettingKeys.Deployment.Mode, deploymentMode, SettingValueType.String),
            Setting(GovernanceSettingKeys.TenantDelegation.LockStorage, lockStorage, SettingValueType.Boolean),
            Setting(GovernanceSettingKeys.Storage.Provider, provider, SettingValueType.String),
            Setting(GovernanceSettingKeys.Storage.DefaultMaxUploadBytes, maxUploadBytes, SettingValueType.Long),
            Setting(GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes, quotaBytes, SettingValueType.Long),
            Setting(GovernanceSettingKeys.Storage.InstanceMaxUploadBytes, instanceMaxUploadBytes, SettingValueType.Long)
        ];

    private static ResolvedSetting Setting<T>(string key, T value, SettingValueType valueType)
        => new()
        {
            Key = key,
            Value = SettingValueSerializer.Serialize(value),
            ValueType = valueType,
            Source = SettingSource.SystemDefault,
            IsLocked = false
        };
}
