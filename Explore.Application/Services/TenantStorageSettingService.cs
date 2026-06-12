// ABOUTME: Service implementation for provider-neutral tenant storage administration.
// ABOUTME: Reads effective settings, redacts secrets, and writes tenant overrides through the settings resolver.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Storage;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Models.Storage;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;

namespace Explore.Application.Services;

public sealed class TenantStorageSettingService : ITenantStorageSettingService
{
    private static readonly string[] StorageSettingKeys =
    [
        GovernanceSettingKeys.Storage.Endpoint,
        GovernanceSettingKeys.Storage.PublicEndpoint,
        GovernanceSettingKeys.Storage.BucketName,
        InfrastructureSecretSettingKeys.Storage.AccessKeyId,
        InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
        GovernanceSettingKeys.Storage.Region,
        GovernanceSettingKeys.Storage.ForcePathStyle,
        GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes,
        GovernanceSettingKeys.Storage.RouteMatrix
    ];

    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IStoragePolicyResolver _storagePolicyResolver;
    private readonly IS3ConfigResolver? _s3ConfigResolver;
    private readonly IStorageUsageCounterRepository _usageCounterRepository;

    public TenantStorageSettingService(
        IHierarchicalSettingsResolver settingsResolver,
        IStoragePolicyResolver storagePolicyResolver,
        IStorageUsageCounterRepository usageCounterRepository,
        IS3ConfigResolver? s3ConfigResolver = null)
    {
        _settingsResolver = settingsResolver;
        _storagePolicyResolver = storagePolicyResolver;
        _s3ConfigResolver = s3ConfigResolver;
        _usageCounterRepository = usageCounterRepository;
    }

    public async Task<TenantStorageSettingsDto> ReadSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var policy = await _storagePolicyResolver.ResolveAsync(tenantId, cancellationToken);
        var settingContext = policy.TenantOverridesAllowed
            ? new SettingContext(TenantId: tenantId)
            : new SettingContext();
        var settings = await ResolveSettingsAsync(settingContext, cancellationToken);
        var envBackedS3Config = _s3ConfigResolver is null ? null : await _s3ConfigResolver.ResolveAsync(cancellationToken);
        var usageCounter = await _usageCounterRepository.GetByTenantAndProviderAsync(
            tenantId,
            policy.Provider,
            cancellationToken);

        return new TenantStorageSettingsDto
        {
            TenantId = tenantId,
            Provider = policy.Provider,
            MaxUploadBytes = policy.MaxUploadBytes,
            TenantQuotaBytes = policy.TenantQuotaBytes,
            IsReadOnly = !policy.TenantOverridesAllowed,
            TenantOverridesAllowed = policy.TenantOverridesAllowed,
            TenantStorageLocked = policy.TenantStorageLocked,
            Routes = MapConfiguredRoutes(settings, policy, isReadOnly: !policy.TenantOverridesAllowed),
            S3Endpoint = ReadString(settings, GovernanceSettingKeys.Storage.Endpoint),
            S3PublicEndpoint = ReadString(settings, GovernanceSettingKeys.Storage.PublicEndpoint),
            S3BucketName = ReadString(settings, GovernanceSettingKeys.Storage.BucketName),
            S3AccessKeyId = string.Empty,
            S3SecretAccessKey = string.Empty,
            S3AccessKeyConfigured = !string.IsNullOrWhiteSpace(ReadString(settings, InfrastructureSecretSettingKeys.Storage.AccessKeyId)) || !string.IsNullOrWhiteSpace(envBackedS3Config?.AccessKeyId),
            S3SecretAccessKeyConfigured = !string.IsNullOrWhiteSpace(ReadString(settings, InfrastructureSecretSettingKeys.Storage.SecretAccessKey)) || !string.IsNullOrWhiteSpace(envBackedS3Config?.SecretAccessKey),
            S3Region = ReadString(settings, GovernanceSettingKeys.Storage.Region, envBackedS3Config?.Region ?? "fsn1"),
            S3ForcePathStyle = ReadBool(settings, GovernanceSettingKeys.Storage.ForcePathStyle, true),
            S3UploadUrlExpirationMinutes = PositiveIntOrDefault(
                ReadInt(settings, GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes, 60),
                60),
            EffectivePolicy = MapPolicy(policy),
            Usage = MapUsage(policy, usageCounter)
        };
    }

    public async Task ApplySettingsAsync(
        Guid tenantId,
        Guid actorUserId,
        TenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var provider = NormalizeProvider(settings.Provider);

        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.Provider,
            provider,
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.DefaultMaxUploadBytes,
            settings.MaxUploadBytes,
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes,
            settings.TenantQuotaBytes,
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.RouteMatrix,
            ToRouteMatrix(settings.Routes),
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.Endpoint,
            TrimOrEmpty(settings.S3Endpoint),
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.PublicEndpoint,
            TrimOrEmpty(settings.S3PublicEndpoint),
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.BucketName,
            TrimOrEmpty(settings.S3BucketName),
            tenantId,
            actorUserId,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.S3AccessKeyId))
        {
            await SetTenantValueAsync(
                InfrastructureSecretSettingKeys.Storage.AccessKeyId,
                settings.S3AccessKeyId.Trim(),
                tenantId,
                actorUserId,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(settings.S3SecretAccessKey))
        {
            await SetTenantValueAsync(
                InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
                settings.S3SecretAccessKey.Trim(),
                tenantId,
                actorUserId,
                cancellationToken);
        }

        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.Region,
            TrimOrEmpty(settings.S3Region),
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.ForcePathStyle,
            settings.S3ForcePathStyle,
            tenantId,
            actorUserId,
            cancellationToken);
        await SetTenantValueAsync(
            GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes,
            settings.S3UploadUrlExpirationMinutes,
            tenantId,
            actorUserId,
            cancellationToken);
    }

    private async Task<Dictionary<string, ResolvedSetting>> ResolveSettingsAsync(
        SettingContext context,
        CancellationToken cancellationToken)
    {
        var resolved = await _settingsResolver.ResolveBatchAsync(StorageSettingKeys, context, cancellationToken);
        return resolved.ToDictionary(setting => setting.Key, setting => setting);
    }

    private async Task SetTenantValueAsync<T>(
        string key,
        T value,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        await _settingsResolver.SetValueAsync(
            key,
            SettingValueSerializer.Serialize(value),
            SettingScope.Tenant,
            tenantId,
            actorUserId,
            cancellationToken);
    }

    private static TenantStorageEffectivePolicyDto MapPolicy(ResolvedStoragePolicy policy)
        => new()
        {
            Provider = policy.Provider,
            MaxUploadBytes = policy.MaxUploadBytes,
            TenantQuotaBytes = policy.TenantQuotaBytes,
            InstanceMaxUploadBytes = policy.InstanceMaxUploadBytes,
            TenantOverridesAllowed = policy.TenantOverridesAllowed,
            TenantStorageLocked = policy.TenantStorageLocked,
            ProviderSource = policy.ProviderSource.ToString(),
            MaxUploadSource = policy.MaxUploadSource.ToString(),
            QuotaSource = policy.QuotaSource.ToString(),
            Routes = MapResolvedRoutes(policy.Routes, isReadOnly: !policy.TenantOverridesAllowed)
        };

    private static List<StorageRouteSettingsDto> MapConfiguredRoutes(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        ResolvedStoragePolicy policy,
        bool isReadOnly)
    {
        var document = settings.TryGetValue(GovernanceSettingKeys.Storage.RouteMatrix, out var setting)
            ? SettingValueSerializer.Deserialize(setting.Value, StorageRouteMatrixDocument.Empty)
            : StorageRouteMatrixDocument.Empty;
        var configured = document.Routes
            .GroupBy(route => NormalizeRouteKey(route.RouteKey))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return policy.Routes
            .Select(route => configured.TryGetValue(route.RouteKey, out var routeSetting)
                ? new StorageRouteSettingsDto
                {
                    RouteKey = route.RouteKey,
                    Provider = NormalizeProvider(routeSetting.Provider),
                    MaxUploadBytes = routeSetting.MaxUploadBytes > 0 ? routeSetting.MaxUploadBytes : route.MaxUploadBytes,
                    ProviderSource = route.ProviderSource.ToString(),
                    MaxUploadSource = route.MaxUploadSource.ToString(),
                    IsReadOnly = isReadOnly
                }
                : MapResolvedRoute(route, isReadOnly))
            .ToList();
    }

    private static StorageRouteMatrixDocument ToRouteMatrix(IEnumerable<StorageRouteSettingsDto> routes)
    {
        var settings = routes
            .GroupBy(route => NormalizeRouteKey(route.RouteKey))
            .Where(group => StorageRouteKeys.All.Contains(group.Key))
            .Select(group => group.First())
            .Select(route => new StorageRouteSetting(
                NormalizeRouteKey(route.RouteKey),
                NormalizeProvider(route.Provider),
                route.MaxUploadBytes))
            .ToList();

        return new StorageRouteMatrixDocument(1, settings);
    }

    private static List<StorageRouteSettingsDto> MapResolvedRoutes(
        IReadOnlyList<ResolvedStorageRoutePolicy> routes,
        bool isReadOnly)
        => routes.Select(route => MapResolvedRoute(route, isReadOnly)).ToList();

    private static StorageRouteSettingsDto MapResolvedRoute(ResolvedStorageRoutePolicy route, bool isReadOnly)
        => new()
        {
            RouteKey = route.RouteKey,
            Provider = route.Provider,
            MaxUploadBytes = route.MaxUploadBytes,
            ProviderSource = route.ProviderSource.ToString(),
            MaxUploadSource = route.MaxUploadSource.ToString(),
            IsReadOnly = isReadOnly
        };

    private static string NormalizeRouteKey(string? routeKey)
        => routeKey?.Trim().ToLowerInvariant() switch
        {
            StorageRouteKeys.Images => StorageRouteKeys.Images,
            StorageRouteKeys.Documents => StorageRouteKeys.Documents,
            StorageRouteKeys.General => StorageRouteKeys.General,
            _ => StorageRouteKeys.General
        };

    private static TenantStorageUsageDto MapUsage(
        ResolvedStoragePolicy policy,
        StorageUsageCounter? counter)
    {
        var usedBytes = counter?.UsedBytes ?? 0;
        var reservedBytes = counter?.ReservedBytes ?? 0;

        return new TenantStorageUsageDto
        {
            Provider = policy.Provider,
            UsedBytes = usedBytes,
            ReservedBytes = reservedBytes,
            QuarantinedBytes = counter?.QuarantinedBytes ?? 0,
            ObjectCount = counter?.ObjectCount ?? 0,
            AvailableBytes = Math.Max(0, policy.TenantQuotaBytes - usedBytes - reservedBytes),
            LastRecalculatedAt = counter?.LastRecalculatedAt
        };
    }

    private static string NormalizeProvider(string? provider)
        => provider?.Trim().ToLowerInvariant() switch
        {
            StorageProviders.S3Compatible => StorageProviders.S3Compatible,
            StorageProviders.Local => StorageProviders.Local,
            _ => StorageProviders.Local
        };

    private static string TrimOrEmpty(string? value)
        => value?.Trim() ?? string.Empty;

    private static string ReadString(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        string defaultValue = "")
        => settings.TryGetValue(key, out var setting)
            ? SettingValueSerializer.DeserializeString(setting.Value, defaultValue)
            : defaultValue;

    private static int ReadInt(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        int defaultValue)
        => settings.TryGetValue(key, out var setting)
            ? SettingValueSerializer.DeserializeInt(setting.Value, defaultValue)
            : defaultValue;

    private static bool ReadBool(
        IReadOnlyDictionary<string, ResolvedSetting> settings,
        string key,
        bool defaultValue)
        => settings.TryGetValue(key, out var setting)
            ? SettingValueSerializer.DeserializeBool(setting.Value, defaultValue)
            : defaultValue;

    private static int PositiveIntOrDefault(int value, int defaultValue)
        => value > 0 ? value : defaultValue;
}
