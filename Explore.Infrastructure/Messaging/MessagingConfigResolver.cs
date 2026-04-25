// ABOUTME: Resolves messaging configuration from the hierarchical settings engine with short-lived cache.
// ABOUTME: Uses system defaults with tenant overrides and lock semantics through IHierarchicalSettingsResolver.

namespace Explore.Infrastructure.Messaging;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public sealed class MessagingConfigResolver : IMessagingConfigResolver
{
    private readonly IHierarchicalSettingsResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MessagingConfigResolver> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "MessagingConfig:";

    public MessagingConfigResolver(
        IHierarchicalSettingsResolver resolver,
        ITenantContext tenantContext,
        IMemoryCache cache,
        ILogger<MessagingConfigResolver> logger)
    {
        _resolver = resolver;
        _tenantContext = tenantContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<MessagingConfiguration> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{CacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out MessagingConfiguration? cached) && cached is not null)
        {
            return cached;
        }

        var config = await ResolveFromSettingsAsync(tenantId, cancellationToken);
        _cache.Set(cacheKey, config, CacheExpiration);
        return config;
    }

    public void InvalidateCache(Guid? tenantId = null)
    {
        if (tenantId.HasValue)
        {
            _cache.Remove($"{CacheKeyPrefix}{tenantId.Value}");
            return;
        }

        _logger.LogInformation("Messaging config cache invalidation requested for all tenants");
    }

    private async Task<MessagingConfiguration> ResolveFromSettingsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var ctx = new SettingContext(TenantId: tenantId);

        var providerStr = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Messaging.Provider, ctx, cancellationToken);
        var enabled = await _resolver.ResolveAsync<bool>(GovernanceSettingKeys.Messaging.Enabled, ctx, cancellationToken);
        var hostName = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Messaging.HostName, ctx, cancellationToken);
        var port = await _resolver.ResolveAsync<int>(GovernanceSettingKeys.Messaging.Port, ctx, cancellationToken);
        var userName = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Messaging.UserName, ctx, cancellationToken);
        var password = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Messaging.Password, ctx, cancellationToken);
        var virtualHost = await _resolver.ResolveAsync<string>(GovernanceSettingKeys.Messaging.VirtualHost, ctx, cancellationToken);
        var maxSize = await _resolver.ResolveAsync<int>(GovernanceSettingKeys.Messaging.MaxInboundMessageBodySize, ctx, cancellationToken);
        var cbThreshold = await _resolver.ResolveAsync<int>(GovernanceSettingKeys.Messaging.CircuitBreakerFailureThreshold, ctx, cancellationToken);
        var cbDuration = await _resolver.ResolveAsync<int>(GovernanceSettingKeys.Messaging.CircuitBreakerBreakDurationSeconds, ctx, cancellationToken);
        var retries = await _resolver.ResolveAsync<int>(GovernanceSettingKeys.Messaging.RetryAttempts, ctx, cancellationToken);
        var otel = await _resolver.ResolveAsync<bool>(GovernanceSettingKeys.Messaging.EnableOpenTelemetry, ctx, cancellationToken);
        var compression = await _resolver.ResolveAsync<bool>(GovernanceSettingKeys.Messaging.EnableCompression, ctx, cancellationToken);

        var provider = ParseProvider(providerStr);

        _logger.LogDebug("Messaging config resolved for tenant {TenantId}: Provider={Provider}, Enabled={Enabled}",
            tenantId, provider, enabled);

        return new MessagingConfiguration
        {
            Provider = provider,
            IsEnabled = enabled,
            HostName = string.IsNullOrWhiteSpace(hostName) ? null : hostName,
            Port = port,
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName,
            Password = string.IsNullOrWhiteSpace(password) ? null : password,
            VirtualHost = string.IsNullOrWhiteSpace(virtualHost) ? "/" : virtualHost,
            MaxInboundMessageBodySize = maxSize,
            CircuitBreakerFailureThreshold = cbThreshold,
            CircuitBreakerBreakDurationSeconds = cbDuration,
            RetryAttempts = retries,
            EnableOpenTelemetry = otel,
            EnableCompression = compression
        };
    }

    private static MessagingProviderEnum ParseProvider(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "rabbitmq" => MessagingProviderEnum.RabbitMq,
            "inmemory" => MessagingProviderEnum.InMemory,
            _ => MessagingProviderEnum.None
        };
    }
}
