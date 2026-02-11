// ABOUTME: Contract for resolving the current user's administrative authority across the hierarchy.
// DB-first authority model: identity from claims, authority from database admin tables.

namespace Explore.Application.Contracts.Identity;

/// <summary>
/// Resolves the current user's administrative authority across the Instance > Tenant > Organization hierarchy.
/// Uses a DB-first authority model where identity comes from authenticated claims and
/// authorization comes from database relationships.
/// Implementations should cache the "Authority Profile" for performance (5-minute sliding window).
/// </summary>
public interface IAdminContext
{
    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets whether the current user is an Instance Administrator.
    /// Resolved from the InstanceAdministrators database table.
    /// </summary>
    Task<bool> IsInstanceAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the current user is a Tenant Administrator for the specified tenant.
    /// Resolved strictly from the TenantAdministrators database table.
    /// </summary>
    Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the current user is an Organization Administrator (Creator, CoOwner, or Admin role).
    /// Resolved strictly from the OrganizationMembers database table.
    /// </summary>
    Task<bool> IsOrganizationAdminAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenant IDs where the current user has administrative rights.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAdminTenantIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all organization IDs where the current user has administrative rights.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAdminOrganizationIdsAsync(CancellationToken cancellationToken = default);
}
