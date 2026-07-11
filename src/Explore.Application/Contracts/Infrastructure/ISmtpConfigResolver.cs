// ABOUTME: Contract for resolving SMTP configuration from the cascading settings engine.
// Supports the SaaS multi-tenant hierarchy: Instance admin → Tenant admin.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Resolves SMTP configuration from the cascading settings engine.
/// <para>
/// Resolution order:
/// 1. Check if settings are locked at system level (instance admin enforces SaaS-wide SMTP)
/// 2. Check for tenant-specific override (tenant brings their own SMTP)
/// 3. Fall back to system default
/// </para>
/// <para>
/// This enables flexible SaaS scenarios:
/// - Instance admin locks SMTP → all tenants use the SaaS provider's server
/// - Instance admin unlocks SMTP → tenants can override with their own credentials
/// - Default SMTP is set at instance level → tenants use it unless they override
/// </para>
/// </summary>
public interface ISmtpConfigResolver
{
    /// <summary>
    /// Resolves the effective SMTP configuration for the current tenant.
    /// Returns null if SMTP is not configured (host is empty/missing).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved SMTP configuration, or null if not configured.</returns>
    Task<SmtpConfiguration?> ResolveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached SMTP configuration.
    /// Call after SMTP settings are changed in the admin UI.
    /// </summary>
    /// <param name="tenantId">Tenant to invalidate, or null for all tenants.</param>
    void InvalidateCache(Guid? tenantId = null);
}
