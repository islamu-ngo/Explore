// ABOUTME: Resolves effective storage policy from hierarchical settings.
// ABOUTME: Keeps provider choice, tenant delegation, quotas, and upload ceilings server-authoritative.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models.Storage;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;

namespace Explore.Application.Services;

public sealed class StoragePolicyResolver : IStoragePolicyResolver
{
    internal const long DefaultMaxUploadBytes = 10 * 1024 * 1024;
    internal const long DefaultTenantQuotaBytes = 1024L * 1024 * 1024;
    internal const long DefaultInstanceMaxUploadBytes = 100L * 1024 * 1024;

    private static readonly string[] PolicySettingKeys =
    [
        GovernanceSettingKeys.Deployment.Mode,
        GovernanceSettingKeys.TenantDelegation.LockStorage,
        GovernanceSettingKeys.Storage.Provider,
        GovernanceSettingKeys.Storage.DefaultMaxUploadBytes,
        GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
        GovernanceSettingKeys.Storage.InstanceMaxUploadBytes
    ];

    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IFileStorageProviderResolver _providerResolver;

    public StoragePolicyResolver(
        IHierarchicalSettingsResolver settingsResolver,
        IFileStorageProviderResolver providerResolver)
    {
        _settingsResolver = settingsResolver;
        _providerResolver = providerResolver;
    }

    public async Task<ResolvedStoragePolicy> ResolveAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var instanceSettings = await ResolveSettingsAsync(new SettingContext(), cancellationToken);
        var isMultiTenant = IsMultiTenant(instanceSettings);
        var tenantStorageLocked = ReadBool(instanceSettings, GovernanceSettingKeys.TenantDelegation.LockStorage, defaultValue: true);
        var tenantOverridesAllowed = tenantId.HasValue && (!isMultiTenant || !tenantStorageLocked);

        var effectiveSettings = instanceSettings;
        if (tenantOverridesAllowed)
        {
            effectiveSettings = await ResolveSettingsAsync(new SettingContext(TenantId: tenantId), cancellationToken);
        }

        var instanceMaxUploadBytes = PositiveOrDefault(
            ReadLong(instanceSettings, GovernanceSettingKeys.Storage.InstanceMaxUploadBytes, DefaultInstanceMaxUploadBytes),
            DefaultInstanceMaxUploadBytes);
        var requestedMaxUploadBytes = PositiveOrDefault(
            ReadLong(effectiveSettings, GovernanceSettingKeys.Storage.DefaultMaxUploadBytes, DefaultMaxUploadBytes),
            DefaultMaxUploadBytes);
        var maxUploadBytes = Math.Min(requestedMaxUploadBytes, instanceMaxUploadBytes);
        var tenantQuotaBytes = PositiveOrDefault(
            ReadLong(effectiveSettings, GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes, DefaultTenantQuotaBytes),
            DefaultTenantQuotaBytes);

        return new ResolvedStoragePolicy(
            tenantId,
            NormalizeProvider(ReadString(effectiveSettings, GovernanceSettingKeys.Storage.Provider, StorageProviders.Local)),
            maxUploadBytes,
            tenantQuotaBytes,
            instanceMaxUploadBytes,
            tenantOverridesAllowed,
            tenantStorageLocked,
            SourceOf(effectiveSettings, GovernanceSettingKeys.Storage.Provider),
            SourceOf(effectiveSettings, GovernanceSettingKeys.Storage.DefaultMaxUploadBytes),
            SourceOf(effectiveSettings, GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes));
    }

    public async Task<IFileStorageProvider> ResolveProviderAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var policy = await ResolveAsync(tenantId, cancellationToken);
        return _providerResolver.GetRequired(policy.Provider);
    }

    private async Task<Dictionary<string, ResolvedSetting>> ResolveSettingsAsync(
        SettingContext context,
        CancellationToken cancellationToken)
    {
        var resolved = await _settingsResolver.ResolveBatchAsync(PolicySettingKeys, context, cancellationToken);
        return resolved.ToDictionary(setting => setting.Key, setting => setting);
    }

    private static bool IsMultiTenant(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        var deploymentMode = ReadString(settings, GovernanceSettingKeys.Deployment.Mode, "SingleTenant");
        return deploymentMode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProvider(string? provider)
        => provider?.Trim().ToLowerInvariant() switch
        {
            StorageProviders.S3Compatible => StorageProviders.S3Compatible,
            StorageProviders.Local => StorageProviders.Local,
            _ => StorageProviders.Local
        };

    private static long PositiveOrDefault(long value, long defaultValue)
        => value > 0 ? value : defaultValue;

    private static bool ReadBool(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        bool defaultValue)
        => settings.TryGetValue(key, out var setting)
            ? SettingValueSerializer.DeserializeBool(setting.Value, defaultValue)
            : defaultValue;

    private static long ReadLong(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        long defaultValue)
        => settings.TryGetValue(key, out var setting)
            ? SettingValueSerializer.DeserializeLong(setting.Value, defaultValue)
            : defaultValue;

    private static string ReadString(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        string defaultValue)
        => settings.TryGetValue(key, out var setting)
            ? SettingValueSerializer.DeserializeString(setting.Value, defaultValue)
            : defaultValue;

    private static SettingSource SourceOf(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key)
        => settings.TryGetValue(key, out var setting) ? setting.Source : SettingSource.SystemDefault;
}
