// ABOUTME: Resolves SMTP configuration from the cascading settings engine.
// Supports SaaS multi-tenant hierarchy: instance admin can lock settings or let tenants override.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Constants;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Mail;

/// <summary>
/// Resolves SMTP settings from the cascading settings engine (SystemSetting → TenantSetting).
/// <para>
/// SaaS scenarios supported:
/// - Instance admin locks SMTP settings → all tenants use the SaaS provider's SMTP server
/// - Instance admin leaves SMTP unlocked → tenants can bring their own SMTP credentials
/// - Default SMTP at instance level → tenants use it unless they override
/// </para>
/// </summary>
public class SmtpConfigResolver : ISmtpConfigResolver
{
    private readonly ISettingsResolver _settingsResolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SmtpConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "SmtpConfig:";

    public SmtpConfigResolver(
        ISettingsResolver settingsResolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<SmtpConfigResolver> logger)
    {
        _settingsResolver = settingsResolver;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SmtpConfiguration?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out SmtpConfiguration? cached))
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
            _logger.LogInformation("SMTP config cache invalidation requested for all tenants");
        }
    }

    private async Task<SmtpConfiguration?> ResolveFromSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // ISettingsResolver handles the cascade:
        // 1. If setting is IsLocked at system level → uses system value (instance admin control)
        // 2. If tenant has an override → uses tenant value (tenant brings own SMTP)
        // 3. Falls back to system default

        var host = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.EmailSmtpHost, tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogDebug("SMTP not configured for tenant {TenantId}: host is empty", tenantId);
            return null;
        }

        var fromAddress = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.EmailFromAddress, tenantId, cancellationToken);
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogDebug("SMTP not configured for tenant {TenantId}: from_address is empty", tenantId);
            return null;
        }

        var port = await _settingsResolver.GetSettingAsync<int>(GovernanceSettingKeys.EmailSmtpPort, tenantId, cancellationToken);
        var securityStr = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.EmailSmtpSecurity, tenantId, cancellationToken);
        var timeout = await _settingsResolver.GetSettingAsync<int>(GovernanceSettingKeys.EmailSmtpTimeoutSeconds, tenantId, cancellationToken);

        return new SmtpConfiguration
        {
            Host = host,
            Port = port > 0 ? port : 587,
            Username = await _settingsResolver.GetSettingAsync<string>(InfrastructureSecretSettingKeys.Email.SmtpUsername, tenantId, cancellationToken),
            Password = await _settingsResolver.GetSettingAsync<string>(InfrastructureSecretSettingKeys.Email.SmtpPassword, tenantId, cancellationToken),
            Security = Enum.TryParse<SmtpSecurityMode>(securityStr, ignoreCase: true, out var security)
                ? security
                : SmtpSecurityMode.StartTls,
            FromAddress = fromAddress,
            FromName = await _settingsResolver.GetSettingAsync<string>(GovernanceSettingKeys.EmailFromName, tenantId, cancellationToken) ?? "Explore",
            TimeoutSeconds = timeout > 0 ? timeout : 30,
            SkipCertificateValidation = await _settingsResolver.GetSettingAsync<bool>(GovernanceSettingKeys.EmailSmtpSkipCertValidation, tenantId, cancellationToken)
        };
    }
}
