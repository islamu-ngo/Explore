// ABOUTME: Unified contract for async-safe deployment mode resolution across middleware and filters.
// ABOUTME: Replaces static volatile cache and inline DB queries with a single shared provider.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Provides async-safe deployment mode resolution from config, cache, or database.
/// All consumers (middleware, filters, handlers) use this single interface.
/// Registered as Singleton; uses IServiceScopeFactory internally for scoped DB access.
/// </summary>
public interface IDeploymentModeProvider
{
    /// <summary>
    /// Returns the current effective deployment mode.
    /// Resolution order: static config → distributed cache → database.
    /// </summary>
    Task<DeploymentMode> GetCurrentModeAsync(CancellationToken ct = default);

    /// <summary>Returns true when the current mode is SingleTenant.</summary>
    Task<bool> IsSingleTenantAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes the cached mode. The next call re-reads from the database.
    /// Call this after a deployment mode change has been committed.
    /// </summary>
    Task InvalidateCacheAsync();
}
