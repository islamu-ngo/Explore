// ABOUTME: Singleton deployment mode provider using IOptionsMonitor + IDistributedCache + DB fallback.
// ABOUTME: Replaces the static volatile cache in ApiTenantResolutionMiddleware with a proper DI-managed pattern.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

public sealed class DeploymentModeProvider : IDeploymentModeProvider
{
    internal const string CacheKey = "DeploymentMode:Current";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly IOptionsMonitor<DeploymentSettings> _settings;
    private readonly IDistributedCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeploymentModeProvider> _logger;

    public DeploymentModeProvider(
        IOptionsMonitor<DeploymentSettings> settings,
        IDistributedCache cache,
        IServiceScopeFactory scopeFactory,
        ILogger<DeploymentModeProvider> logger)
    {
        _settings = settings;
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<DeploymentMode> GetCurrentModeAsync(CancellationToken ct = default)
    {
        // Layer 1: static config (env var / appsettings) — highest priority
        if (_settings.CurrentValue.IsSingleTenant)
            return DeploymentMode.SingleTenant;

        // Layer 2: distributed cache
        var cached = await TryGetCachedModeAsync(ct);
        if (cached is not null)
            return cached.Value;

        // Layer 3: database (scoped repo accessed via factory)
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<IInstanceBootstrapStateRepository>();
        var bootstrap = await repo.GetCurrent();

        var mode = bootstrap?.IsCompleted == true
            && Enum.TryParse<DeploymentMode>(bootstrap.SelectedDeploymentMode, out var dbMode)
            ? dbMode
            : DeploymentMode.MultiTenant;

        await TrySetCachedModeAsync(mode, ct);

        _logger.LogDebug("Deployment mode resolved from DB: {Mode}", mode);
        return mode;
    }

    public async Task<bool> IsSingleTenantAsync(CancellationToken ct = default)
        => await GetCurrentModeAsync(ct) == DeploymentMode.SingleTenant;

    public async Task InvalidateCacheAsync()
        => await TryRemoveCachedModeAsync();

    private async Task<DeploymentMode?> TryGetCachedModeAsync(CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetStringAsync(CacheKey, ct);
            return cached is not null && Enum.TryParse<DeploymentMode>(cached, out var cachedMode)
                ? cachedMode
                : null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Distributed cache unavailable while resolving deployment mode. Falling back to database.");
            return null;
        }
    }

    private async Task TrySetCachedModeAsync(DeploymentMode mode, CancellationToken ct)
    {
        try
        {
            await _cache.SetStringAsync(
                CacheKey,
                mode.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                },
                ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Distributed cache unavailable while storing deployment mode. Continuing without cache.");
        }
    }

    private async Task TryRemoveCachedModeAsync()
    {
        try
        {
            await _cache.RemoveAsync(CacheKey);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Distributed cache unavailable while invalidating deployment mode cache.");
        }
    }
}
