// ABOUTME: Contract for extracting authorization metadata from a resource (DTO) instance.
// ABOUTME: Used by HATEOAS link policies and authorization evaluators to build Cerbos check requests.

namespace Explore.Application.Authorization;

/// <summary>
/// Describes how to extract authorization metadata from a resource instance.
/// <para>
/// Implementations live in the Application layer alongside their DTOs and are consumed by:
/// <list type="bullet">
///   <item>HATEOAS link policies (API layer) — to build permission checks for links</item>
///   <item><see cref="HateoasAuthorizationEvaluator"/> — to construct batch authorization requests</item>
///   <item>Authorization behaviors — to extract resource context from command/query payloads</item>
/// </list>
/// </para>
/// <para>
/// Each resource kind should have one descriptor per DTO type that participates in authorization.
/// Descriptors are transport-neutral — they know nothing about HTTP, routes, or link generation.
/// </para>
/// </summary>
/// <typeparam name="TResource">
/// The DTO or resource type from which authorization metadata is extracted.
/// Contravariant so a descriptor for a base DTO type can serve derived types.
/// </typeparam>
public interface IAuthorizableResourceDescriptor<in TResource>
{
    /// <summary>
    /// The Cerbos resource kind string (e.g., ResourceKinds.Event, ResourceKinds.Organization).
    /// Must match a constant from <see cref="ResourceKinds"/>.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Extracts the unique resource identifier used for authorization.
    /// Typically the entity's primary key as a string.
    /// </summary>
    string GetResourceId(TResource resource);

    /// <summary>
    /// Extracts resource attributes passed to Cerbos for policy evaluation.
    /// Common attributes: tenantId, organizationId, actorId, ownerId.
    /// Returns an empty dictionary when no attributes are needed.
    /// </summary>
    IReadOnlyDictionary<string, object> GetResourceAttributes(TResource resource);

    /// <summary>
    /// Extracts the authorization scope for per-tenant/org policy resolution.
    /// </summary>
    AuthorizationScope GetScope(TResource resource);
}
