// ABOUTME: Provider-agnostic contract for authorization decisions across the application.
// ABOUTME: Implemented by CerbosAuthorizationProvider (ABAC via PDP) and LocalAuthorizationProvider (DB-driven RBAC).

namespace Explore.Application.Contracts.Infrastructure;

using System.Collections.Generic;
using Explore.Application.Authorization;

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

/// <summary>
/// Transport-neutral authorization check request.
/// Captures all metadata needed for a single Cerbos resource check.
/// </summary>
/// <param name="ResourceKind">Resource kind string from <see cref="ResourceKinds"/>.</param>
/// <param name="ResourceId">Unique resource identifier (typically entity primary key).</param>
/// <param name="Action">Action string from <see cref="AuthorizationActions"/>.</param>
/// <param name="ResourceAttributes">Additional attributes for policy evaluation (e.g., tenantId, ownerId).</param>
/// <param name="Scope">Tenant/org scope for per-tenant policy resolution. Null for unscoped checks.</param>
public sealed record AuthorizationCheck(
    string ResourceKind,
    string ResourceId,
    string Action,
    IReadOnlyDictionary<string, object>? ResourceAttributes = null,
    AuthorizationScope? Scope = null)
{
    /// <summary>
    /// Generates a deduplication key for batch authorization requests.
    /// Two checks with the same key are semantically identical and only need
    /// to be evaluated once. Key is based on resource kind, id, and action —
    /// the triple that uniquely identifies a Cerbos check.
    /// </summary>
    public string ToDeduplicationKey() => $"{ResourceKind}|{ResourceId}|{Action}";
}
