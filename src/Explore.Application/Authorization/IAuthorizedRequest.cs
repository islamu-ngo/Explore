// ABOUTME: Legacy marker interface for MediatR requests that require authorization.
// ABOUTME: Deprecated in favor of [AuthorizeResource] attribute. Zero production implementations exist.

using System.ComponentModel;

namespace Explore.Application.Authorization;

/// <summary>
/// Legacy marker interface for MediatR requests that require authorization checks.
/// <para>
/// <b>Deprecated</b>: Use <see cref="AuthorizeResourceAttribute"/> instead (optionally with <see cref="ISecureRequest"/>
/// for dynamic resource context). All production commands use the attribute path. This interface is retained only
/// for backward compatibility in <see cref="Behaviors.AuthorizationBehavior{TRequest, TResponse}"/>.
/// </para>
/// </summary>
[Obsolete("Use [AuthorizeResource] attribute (+ ISecureRequest for dynamic context) instead. No production implementations exist.")]
public interface IAuthorizedRequest
{
    /// <summary>
    /// The resource kind (e.g., ResourceKinds.InstanceSetting, ResourceKinds.TenantSetting, ResourceKinds.Organization).
    /// </summary>
    string ResourceKind { get; }

    /// <summary>
    /// The specific resource identifier being accessed.
    /// </summary>
    string ResourceId { get; }

    /// <summary>
    /// The action being performed (e.g., "view", "update", "delete").
    /// </summary>
    string Action { get; }

    /// <summary>
    /// Additional resource attributes for policy evaluation (e.g., tenantId, isLocked).
    /// </summary>
    IDictionary<string, object>? ResourceAttributes => null;
}
