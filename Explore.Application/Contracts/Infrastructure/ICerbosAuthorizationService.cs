// ABOUTME: Contract for the Cerbos-based authorization service.
// Abstracts the Cerbos PDP so implementations can use either the real gRPC SDK or a DB-only fallback.

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Authorization service that evaluates access control decisions.
/// In production, delegates to Cerbos PDP via gRPC. Falls back to database-driven checks
/// when Cerbos is unavailable (e.g., ATProto/PDS-only deployments).
/// </summary>
public interface ICerbosAuthorizationService
{
    /// <summary>
    /// Checks if the current user is allowed to perform an action on a resource.
    /// </summary>
    /// <param name="resourceKind">The type of resource (e.g., "instance_setting", "tenant_setting", "organization").</param>
    /// <param name="resourceId">The specific resource identifier.</param>
    /// <param name="action">The action being attempted (e.g., "view", "update", "delete").</param>
    /// <param name="resourceAttributes">Additional attributes about the resource (e.g., tenantId, isLocked).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the action is allowed, false if denied.</returns>
    Task<bool> IsAllowedAsync(
        string resourceKind,
        string resourceId,
        string action,
        IDictionary<string, object>? resourceAttributes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user can modify a specific setting, considering lock semantics.
    /// Convenience method that builds the resource context from the setting key and scope.
    /// </summary>
    /// <param name="settingKey">The setting key (e.g., "events.require_approval").</param>
    /// <param name="action">The action (e.g., "update").</param>
    /// <param name="tenantId">The tenant scope (null for instance-level).</param>
    /// <param name="organizationId">The organization scope (null if not org-level).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the setting modification is allowed.</returns>
    Task<bool> CheckSettingAccessAsync(
        string settingKey,
        string action,
        Guid? tenantId = null,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default);
}
