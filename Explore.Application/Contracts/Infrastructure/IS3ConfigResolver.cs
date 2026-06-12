// ABOUTME: Contract for resolving S3 storage configuration from the cascading settings engine.
// Supports the SaaS multi-tenant hierarchy: Instance admin → Tenant admin.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Resolves S3 storage configuration from the cascading settings engine.
/// <para>
/// Resolution order:
/// 1. Check if settings are locked at system level (instance admin enforces SaaS-wide storage)
/// 2. Check for tenant-specific override (tenant brings their own S3 storage)
/// 3. Fall back to system default
/// </para>
/// <para>
/// This enables flexible SaaS scenarios:
/// - Instance admin locks S3 settings → all tenants use the SaaS provider's storage
/// - Instance admin unlocks S3 settings → tenants can override with their own credentials
/// - Default S3 config is set at instance level → tenants use it unless they override
/// </para>
/// </summary>
public interface IS3ConfigResolver
{
    /// <summary>
    /// Resolves the effective S3 configuration for the current tenant.
    /// Returns null if S3 is not configured (endpoint is empty/missing).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved S3 configuration, or null if not configured.</returns>
    Task<S3Configuration?> ResolveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when the effective S3 configuration has all required fields.
    /// </summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached S3 configuration.
    /// Call after S3 settings are changed in the admin UI.
    /// </summary>
    /// <param name="tenantId">Tenant to invalidate, or null for all tenants.</param>
    void InvalidateCache(Guid? tenantId = null);
}
