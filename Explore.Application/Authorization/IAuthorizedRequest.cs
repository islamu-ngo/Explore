// ABOUTME: Marker interface for MediatR requests that require authorization.
// ABOUTME: Requests implementing this provide resource context for the AuthorizationBehavior pipeline.

namespace Explore.Application.Authorization;

/// <summary>
/// Marker interface for MediatR requests that require authorization checks.
/// The AuthorizationBehavior pipeline behavior inspects requests for this interface
/// and calls IAuthorizationProvider before the handler executes.
/// </summary>
public interface IAuthorizedRequest
{
    /// <summary>
    /// The resource kind (e.g., "instance_setting", "tenant_setting", "organization").
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
