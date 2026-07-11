// ABOUTME: Short-TTL cached onboarding status probe shared by middleware, cookie events, and admin enrichment.
// ABOUTME: Fetches GET /api/InstanceOnboarding/status (AllowAnonymous, fast) and caches the result to avoid request storms.

using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace Explore.Blazor.Services;

/// <summary>
/// Cached onboarding status probe for the BFF. Several components need to know whether
/// instance onboarding is complete (auth entry gate, stale-cookie redirect target,
/// admin enrichment skip). Each sharing the same short-TTL cache prevents redundant
/// outbound calls and cascade timeouts during cold start.
/// </summary>
public interface IBffOnboardingStatusProvider
{
    /// <summary>
    /// Returns the latest known onboarding status, fetching fresh when cache is cold or expired.
    /// Never throws — returns a conservative <see cref="BffOnboardingStatus.Unknown"/> on error
    /// so callers default to the safest behavior (treat as incomplete).
    /// </summary>
    Task<BffOnboardingStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached status. Call after onboarding completes so the next request
    /// sees the fresh state immediately.
    /// </summary>
    void Invalidate();
}

/// <summary>Snapshot of onboarding status used by gating logic.</summary>
public sealed record BffOnboardingStatus(bool IsCompleted, bool IsSetupModeActive, bool Known)
{
    /// <summary>Default conservative value when the API is unreachable — treat as not-completed.</summary>
    public static BffOnboardingStatus Unknown { get; } = new(IsCompleted: false, IsSetupModeActive: true, Known: false);
}

public sealed class BffOnboardingStatusProvider : IBffOnboardingStatusProvider
{
    private const string CacheKey = "BffOnboardingStatus";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BffOnboardingStatusProvider> _logger;

    public BffOnboardingStatusProvider(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<BffOnboardingStatusProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<BffOnboardingStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<BffOnboardingStatus>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var apiClient = scope.ServiceProvider.GetRequiredService<IEventApiClient>();
            var dto = await apiClient.GetInstanceOnboardingStatusAsync(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var status = new BffOnboardingStatus(
                IsCompleted: dto.IsCompleted == true,
                IsSetupModeActive: dto.IsSetupModeActive == true,
                Known: true);

            _cache.Set(CacheKey, status, CacheTtl);
            return status;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Onboarding status probe failed; treating as unknown.");
            return BffOnboardingStatus.Unknown;
        }
    }

    public void Invalidate() => _cache.Remove(CacheKey);
}
