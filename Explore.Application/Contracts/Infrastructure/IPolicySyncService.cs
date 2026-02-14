// ABOUTME: Contract for synchronizing authorization policies to the Cerbos PDP.
// ABOUTME: Generates policies from Permission/RolePermission data and pushes via Admin API.

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Service that generates Cerbos policies from Permission and RolePermission data,
/// pushes them to the Cerbos Admin API, and broadcasts reload commands to all instances.
/// Used when the authorization provider is set to "cerbos" and custom roles are modified.
/// </summary>
public interface IPolicySyncService
{
    /// <summary>
    /// Generates and pushes all policies from the current Permission and RolePermission state.
    /// Used for full resync (e.g., admin-triggered, initial setup).
    /// </summary>
    Task SyncAllPoliciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and pushes policies affected by a specific role change.
    /// More efficient than full sync for single-role updates.
    /// </summary>
    /// <param name="roleId">The role that was created, updated, or deleted.</param>
    Task SyncRolePoliciesAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a cache reload command to all known Cerbos instances.
    /// Used after critical permission changes that require immediate consistency.
    /// </summary>
    Task ReloadAllInstancesAsync(CancellationToken cancellationToken = default);
}
