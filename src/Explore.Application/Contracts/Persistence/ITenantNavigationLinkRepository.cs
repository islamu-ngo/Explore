using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

/// <summary>
/// Repository interface for TenantNavigationLink entity.
/// Provides data access operations for tenant navigation links.
/// </summary>
public interface ITenantNavigationLinkRepository : IGenericRepository<TenantNavigationLink, Guid>
{
    /// <summary>
    /// Gets all active navigation links for a specific tenant, ordered by display order.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of navigation links ordered by Order property.</returns>
    Task<List<TenantNavigationLink>> GetByTenantIdOrderedAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a navigation link by ID and verifies it belongs to the specified tenant.
    /// </summary>
    /// <param name="id">The navigation link identifier.</param>
    /// <param name="tenantId">The tenant identifier for verification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The navigation link if found and belongs to tenant, null otherwise.</returns>
    Task<TenantNavigationLink?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum order value for a tenant's navigation links.
    /// Used to determine the next order when creating new links.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The maximum order value, or 0 if no links exist.</returns>
    Task<int> GetMaxOrderByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all navigation links for a specific tenant.
    /// Used during tenant cleanup/deletion.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of links deleted.</returns>
    Task<int> DeleteByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
