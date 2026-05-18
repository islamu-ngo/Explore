// ABOUTME: Provider-agnostic contract for authorization decisions across the application.
// ABOUTME: Implemented by CerbosAuthorizationProvider (ABAC via PDP) and LocalAuthorizationProvider (DB-driven RBAC).

namespace Explore.Application.Contracts.Infrastructure;

using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    /// to be evaluated once. Key includes resource identity, action, scope,
    /// and canonical resource attributes so scoped/attribute-sensitive checks
    /// cannot collapse into each other.
    /// </summary>
    public string ToDeduplicationKey()
    {
        var builder = new StringBuilder();

        AppendSegment(builder, ResourceKind);
        AppendSegment(builder, ResourceId);
        AppendSegment(builder, Action);
        AppendScope(builder, Scope);
        AppendAttributes(builder, ResourceAttributes);

        return builder.ToString();
    }

    private static void AppendScope(StringBuilder builder, AuthorizationScope? scope)
    {
        AppendSegment(builder, scope?.TenantId ?? string.Empty);
        AppendSegment(builder, scope?.OrganizationId ?? string.Empty);
    }

    private static void AppendAttributes(StringBuilder builder, IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            AppendSegment(builder, string.Empty);
            return;
        }

        AppendSegment(builder, attributes.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var pair in attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            AppendSegment(builder, pair.Key);
            AppendSegment(builder, NormalizeAttributeValue(pair.Value));
        }
    }

    private static string NormalizeAttributeValue(object? value)
    {
        if (value is null)
            return "<null>";

        var rendered = value switch
        {
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        return $"{value.GetType().FullName}:{rendered}";
    }

    private static void AppendSegment(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
        builder.Append('|');
    }
}
