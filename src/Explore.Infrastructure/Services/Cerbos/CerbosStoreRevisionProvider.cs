// ABOUTME: Observes and caches the Cerbos policy store revision for decision stamping and fail-closed gating.
// ABOUTME: Never throws for provider unavailability; reports the revision as unknown instead.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services;

/// <summary>
/// Reports which Cerbos policy revision the PDP is currently serving.
/// <para>
/// The observation comes from <see cref="IPolicyPackageService.GetStatusAsync"/> rather than a second
/// Admin API client, so package health and store revision are always read from the same place and cannot
/// disagree. The result is cached for a bounded interval because this is consulted on the decision path.
/// </para>
/// <para>
/// A revision is reported as observed only when the package is healthy <em>and</em> a revision came back.
/// A healthy package whose revision could not be read is still uncertain: the store may hold an in-place
/// edit that a policy listing cannot see.
/// </para>
/// </summary>
public sealed class CerbosStoreRevisionProvider : IAuthorizationRevisionProvider
{
    private const string CacheKey = "AuthorizationProvider_ObservedRevision";

    /// <summary>
    /// How long an observation is reused. Matches the provider-mode cache so an operator who republishes
    /// the package and switches mode sees both take effect on the same bounded horizon.
    /// </summary>
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private readonly IPolicyPackageService _policyPackageService;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CerbosStoreRevisionProvider> _logger;

    public CerbosStoreRevisionProvider(
        IPolicyPackageService policyPackageService,
        IMemoryCache cache,
        ILogger<CerbosStoreRevisionProvider> logger,
        TimeProvider? timeProvider = null)
    {
        _policyPackageService = policyPackageService;
        _cache = cache;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<AuthorizationRevision> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out AuthorizationRevision? cached) && cached is not null)
            return cached;

        var revision = await ObserveAsync(cancellationToken);

        // Cache the uncertain result too. Without that, an unreachable Admin API would be re-queried on
        // every decision batch, turning a policy-store outage into a request-path stall.
        _cache.Set(CacheKey, revision, CacheDuration);
        return revision;
    }

    /// <inheritdoc />
    public void Invalidate()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Authorization policy revision cache invalidated; it will be re-observed on the next decision.");
    }

    private async ValueTask<AuthorizationRevision> ObserveAsync(CancellationToken cancellationToken)
    {
        var observedAt = _timeProvider.GetUtcNow();

        PolicyPackageStatusResult status;
        try
        {
            status = await _policyPackageService.GetStatusAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Could not establish the Cerbos policy revision. Sensitive actions will fail closed. FailureType={FailureType}",
                ex.GetType().Name);
            return AuthorizationRevision.Unknown(observedAt);
        }

        if (!status.IsHealthy || status.ObservedRevision is not { } revision)
        {
            _logger.LogWarning(
                "Cerbos policy revision is not established. IssueCode={IssueCode} HasRevision={HasRevision}",
                status.IssueCode,
                status.ObservedRevision is not null);
            return AuthorizationRevision.Unknown(observedAt);
        }

        return AuthorizationRevision.Observed(revision, observedAt);
    }
}
