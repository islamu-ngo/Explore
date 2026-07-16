// ABOUTME: Authorization request interfaces for dynamic resource context and persisted owner binding.
// ABOUTME: Lets the pipeline enrich policy inputs and carry an authorized owner snapshot into atomic writes.

namespace Explore.Application.Authorization;

/// <summary>
/// Optional companion interface for commands decorated with <see cref="AuthorizeResourceAttribute"/>.
/// When a request implements this interface, the <see cref="Behaviors.AuthorizationBehavior{TRequest,TResponse}"/>
/// pulls dynamic resource context (ResourceId, ResourceAttributes) from the request instance
/// instead of using static defaults (typeof(TRequest).Name, null).
///
/// This is NOT a third authorization path — it enhances Path 2 ([AuthorizeResource]).
/// Existing [AuthorizeResource] commands without ISecureRequest continue to work unchanged.
/// </summary>
public interface ISecureRequest
{
    /// <summary>
    /// The specific resource identifier for the authorization check (e.g., an OrganizationId).
    /// Returns null to fall back to typeof(TRequest).Name.
    /// </summary>
    string? ResourceId => null;

    /// <summary>
    /// Additional resource attributes for policy evaluation (e.g., tenantId, isLocked).
    /// Returns null when no additional attributes are needed.
    /// </summary>
    IDictionary<string, object>? ResourceAttributes => null;
}

public interface IPersistedUserOwnerBoundRequest
{
    Guid? ExpectedOwnerUserId { get; set; }
}
