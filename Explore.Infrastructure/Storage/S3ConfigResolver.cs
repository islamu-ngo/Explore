// ABOUTME: Resolves S3 storage configuration from hierarchical settings (DB) with IConfiguration fallback.
// ABOUTME: Supports secret manager (Infisical/env vars) as base layer and DB overrides for UI-configured values.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Storage;

/// <summary>
/// Resolves S3 settings with a two-tier fallback:
/// <list type="number">
///   <item>Cascading settings engine (SystemSetting → TenantSetting) — values from admin UI</item>
///   <item>IConfiguration (Infisical / environment variables) — values from secret manager</item>
/// </list>
/// <para>
/// This allows self-hosters to choose either approach:
/// - Secret manager: configure Infisical/env vars, values refresh without restart
/// - Admin UI: configure via Instance Settings, values stored in database
/// - Both: secret manager provides base, DB overrides take precedence
/// </para>
/// </summary>
public class S3ConfigResolver : IS3ConfigResolver
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3ConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "S3Config:";

    public S3ConfigResolver(
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<S3ConfigResolver> logger)
    {
        _resolver = resolver;
        _tenantContext = tenantContext;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<S3Configuration?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out S3Configuration? cached))
        {
            return cached;
        }

        var config = await ResolveFromSettingsAsync(tenantId, cancellationToken);

        if (config is not null)
        {
            _cache.Set(cacheKey, config, CacheExpiration);
        }

        return config;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
        => await ResolveAsync(cancellationToken) is not null;

    public void InvalidateCache(Guid? tenantId = null)
    {
        if (tenantId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}{tenantId.Value}");
        }
        else
        {
            // Can't enumerate MemoryCache keys — callers should invalidate specific tenants.
            // For a full flush, the SettingsResolver also has its own cache invalidation.
            _logger.LogInformation("S3 config cache invalidation requested for all tenants");
        }
    }

    private async Task<S3Configuration?> ResolveFromSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Resolution order for each field:
        // 1. Cascading settings engine (SystemSetting → TenantSetting) — DB values from admin UI
        // 2. IConfiguration fallback (Infisical / env vars) — secret manager values
        // This means DB-configured values always take precedence over secret manager values.

        var endpoint = await ResolveStringAsync(
            GovernanceSettingKeys.Storage.Endpoint,
            tenantId,
            cancellationToken,
            "S3Settings:Endpoint",
            "Storage:S3Endpoint",
            "Storage:S3:Endpoint",
            "storage.s3.endpoint",
            "STORAGE_S3_ENDPOINT");
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: endpoint is empty", tenantId);
            return null;
        }

        var bucketName = await ResolveStringAsync(
            GovernanceSettingKeys.Storage.BucketName,
            tenantId,
            cancellationToken,
            "S3Settings:BucketName",
            "Storage:S3BucketName",
            "Storage:S3:BucketName",
            "storage.s3.bucket_name",
            "STORAGE_S3_BUCKET_NAME");
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: bucket_name is empty", tenantId);
            return null;
        }

        var accessKeyId = await ResolveStringAsync(
            InfrastructureSecretSettingKeys.Storage.AccessKeyId,
            tenantId,
            cancellationToken,
            "S3Settings:AccessKeyId",
            "Storage:S3AccessKeyId",
            "Storage:S3:AccessKeyId",
            "storage.s3.access_key_id",
            "STORAGE_S3_ACCESS_KEY_ID");
        if (string.IsNullOrWhiteSpace(accessKeyId))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: access_key_id is empty", tenantId);
            return null;
        }

        var secretAccessKey = await ResolveStringAsync(
            InfrastructureSecretSettingKeys.Storage.SecretAccessKey,
            tenantId,
            cancellationToken,
            "S3Settings:SecretAccessKey",
            "Storage:S3SecretAccessKey",
            "Storage:S3:SecretAccessKey",
            "storage.s3.secret_access_key",
            "STORAGE_S3_SECRET_ACCESS_KEY");
        if (string.IsNullOrWhiteSpace(secretAccessKey))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: secret_access_key is empty", tenantId);
            return null;
        }

        var uploadExpiration = await _resolver.ResolveAsync<int>(GovernanceSettingKeys.Storage.UploadUrlExpirationMinutes, new SettingContext(TenantId: tenantId), cancellationToken);
        var forcePathStyle = await _resolver.ResolveAsync<bool>(GovernanceSettingKeys.Storage.ForcePathStyle, new SettingContext(TenantId: tenantId), cancellationToken);

        var region = await ResolveStringAsync(
            GovernanceSettingKeys.Storage.Region,
            tenantId,
            cancellationToken,
            "S3Settings:Region",
            "Storage:S3Region",
            "Storage:S3:Region",
            "storage.s3.region",
            "STORAGE_S3_REGION");
        var publicEndpoint = await ResolveStringAsync(
            GovernanceSettingKeys.Storage.PublicEndpoint,
            tenantId,
            cancellationToken,
            "S3Settings:PublicEndpoint",
            "Storage:S3PublicEndpoint",
            "Storage:S3:PublicEndpoint",
            "storage.s3.public_endpoint",
            "STORAGE_S3_PUBLIC_ENDPOINT");

        return new S3Configuration
        {
            Endpoint = endpoint,
            BucketName = bucketName,
            AccessKeyId = accessKeyId,
            SecretAccessKey = secretAccessKey,
            Region = string.IsNullOrWhiteSpace(region) ? "us-east-1" : region,
            PublicEndpoint = publicEndpoint,
            ForcePathStyle = forcePathStyle,
            UploadUrlExpirationMinutes = uploadExpiration > 0 ? uploadExpiration : 60,
            DownloadUrlExpirationMinutes = 60
        };
    }

    /// <summary>
    /// Resolves a string setting with IConfiguration fallback.
    /// First tries the cascading settings engine (DB), then falls back to IConfiguration (secret manager).
    /// </summary>
    private async Task<string?> ResolveStringAsync(
        string settingKey,
        Guid tenantId,
        CancellationToken cancellationToken,
        params string[] configKeys)
    {
        var dbValue = await _resolver.ResolveAsync<string>(settingKey, new SettingContext(TenantId: tenantId), cancellationToken);
        if (!string.IsNullOrWhiteSpace(dbValue))
        {
            return dbValue;
        }

        // Fallback to IConfiguration (Infisical / environment variables). Keep DB settings authoritative,
        // but accept all platform-supported secret shapes: legacy S3Settings, Infisical /storage mapping,
        // canonical lower-dot keys, and raw environment variable names.
        foreach (var configKey in configKeys)
        {
            var configValue = _configuration[configKey];
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                return configValue;
            }
        }

        return null;
    }
}
