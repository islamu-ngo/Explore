// ABOUTME: Provider-agnostic contract for authorization decisions across the application.
// ABOUTME: Implemented by CerbosAuthorizationProvider (ABAC via PDP) and LocalAuthorizationProvider (DB-driven RBAC).

namespace Explore.Application.Contracts.Infrastructure;

using System.Collections.Generic;

/// <summary>
/// Authorization provider that evaluates access control decisions.
/// Two implementations: CerbosAuthorizationProvider (external PDP) and LocalAuthorizationProvider (DB-driven).
/// Runtime switching is handled by RuntimeAuthorizationProvider wrapper via SystemSetting.
/// </summary>
public interface IAuthorizationProvider
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
    /// Checks permissions for multiple resource/action pairs in a single call.
    /// Result order matches request order.
    /// </summary>
    Task<IReadOnlyList<bool>> IsAllowedBatchAsync(
        IReadOnlyList<AuthorizationCheck> checks,
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

public sealed record AuthorizationCheck(
    string ResourceKind,
    string ResourceId,
    string Action,
    IReadOnlyDictionary<string, object>? ResourceAttributes = null);
