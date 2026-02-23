// ABOUTME: Resolves S3 storage configuration from cascading settings (DB) with IConfiguration fallback.
// ABOUTME: Supports secret manager (Infisical/env vars) as base layer and DB overrides for UI-configured values.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
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
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3ConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "S3Config:";

    public S3ConfigResolver(
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<S3ConfigResolver> logger)
    {
        _settingsResolver = settingsResolver;
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

        var endpoint = await ResolveStringAsync(GovernanceSettingKeys.S3Endpoint, "S3Settings:Endpoint", tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: endpoint is empty", tenantId);
            return null;
        }

        var bucketName = await ResolveStringAsync(GovernanceSettingKeys.S3BucketName, "S3Settings:BucketName", tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: bucket_name is empty", tenantId);
            return null;
        }

        var accessKeyId = await ResolveStringAsync(InfrastructureSecretSettingKeys.Storage.AccessKeyId, "S3Settings:AccessKeyId", tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessKeyId))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: access_key_id is empty", tenantId);
            return null;
        }

        var secretAccessKey = await ResolveStringAsync(InfrastructureSecretSettingKeys.Storage.SecretAccessKey, "S3Settings:SecretAccessKey", tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(secretAccessKey))
        {
            _logger.LogDebug("S3 not configured for tenant {TenantId}: secret_access_key is empty", tenantId);
            return null;
        }

        var uploadExpiration = await _settingsResolver.GetSettingAsync<int>(GovernanceSettingKeys.S3UploadUrlExpirationMinutes, tenantId, cancellationToken);
        var forcePathStyle = await _settingsResolver.GetSettingAsync<bool>(GovernanceSettingKeys.S3ForcePathStyle, tenantId, cancellationToken);

        var region = await ResolveStringAsync(GovernanceSettingKeys.S3Region, "S3Settings:Region", tenantId, cancellationToken);
        var publicEndpoint = await ResolveStringAsync(GovernanceSettingKeys.S3PublicEndpoint, "S3Settings:PublicEndpoint", tenantId, cancellationToken);

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
        string settingKey, string configKey, Guid tenantId, CancellationToken cancellationToken)
    {
        var dbValue = await _settingsResolver.GetSettingAsync<string>(settingKey, tenantId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(dbValue))
        {
            return dbValue;
        }

        // Fallback to IConfiguration (Infisical / environment variables)
        return _configuration[configKey];
    }
}
