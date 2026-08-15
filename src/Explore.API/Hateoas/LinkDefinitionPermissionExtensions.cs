// ABOUTME: Extension methods that add permission requirements to HATEOAS link definitions.
// ABOUTME: Resolves resource kinds from DTO types via registry and attaches action strings.

namespace Explore.API.Hateoas;

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Hateoas;

/// <summary>
/// Extensions for attaching permission checks to <see cref="LinkDefinition"/> instances.
/// Prefer the string-action overloads with <see cref="AuthorizationActions"/> constants.
/// </summary>
public static class LinkDefinitionPermissionExtensions
{
    /// <summary>
    /// Attaches a permission check using an <see cref="AuthorizationActions"/> string constant.
    /// Uses the specified <paramref name="resourceKind"/> directly.
    /// </summary>
    public static LinkDefinition RequirePermission(
        this LinkDefinition definition,
        string action,
        string resourceKind,
        string? resourceId = null,
        IReadOnlyDictionary<string, object>? resourceAttributes = null,
        AuthorizationScope? scope = null,
        IAuthorizationFacts? facts = null)
    {
        return definition.WithPermission(resourceKind, action, resourceId, resourceAttributes, scope, facts);
    }

    /// <summary>
    /// Attaches a permission check using an <see cref="AuthorizationActions"/> string constant.
    /// Resolves the resource kind from the specified <paramref name="resourceType"/> via the registry.
    /// </summary>
    public static LinkDefinition RequirePermission(
        this LinkDefinition definition,
        string action,
        Type resourceType,
        string? resourceId = null,
        IReadOnlyDictionary<string, object>? resourceAttributes = null,
        AuthorizationScope? scope = null)
    {
        var resourceKind = ResourceDescriptorRegistry.ResolveResourceKind(resourceType);

        return definition.WithPermission(resourceKind, action, resourceId, resourceAttributes, scope);
    }

    /// <summary>
    /// Attaches a permission check using a resource descriptor to extract authorization metadata.
    /// <para>
    /// Preferred overload for link policies — replaces manual dictionary construction with
    /// centralized, type-safe metadata extraction from the DTO instance.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto)
    /// </code>
    /// </example>
    public static LinkDefinition RequirePermission<TResource>(
        this LinkDefinition definition,
        string action,
        IAuthorizableResourceDescriptor<TResource> descriptor,
        TResource resource)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(resource);

        var facts = descriptor.GetFacts(resource);
        return definition.WithPermission(
            descriptor.Kind,
            action,
            descriptor.GetResourceId(resource),
            facts is null ? descriptor.GetResourceAttributes(resource) : null,
            descriptor.GetScope(resource),
            facts);
    }
}
