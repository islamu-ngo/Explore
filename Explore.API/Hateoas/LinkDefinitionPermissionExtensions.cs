namespace Explore.API.Hateoas;

using Explore.Application.Authorization;
using Explore.Application.Hateoas;

public static class LinkDefinitionPermissionExtensions
{
    public static LinkDefinition RequirePermission<TResource>(
        this LinkDefinition definition,
        PermissionAction action,
        TResource resource,
        string? resourceId = null,
        IReadOnlyDictionary<string, object>? resourceAttributes = null)
        where TResource : class
    {
        ArgumentNullException.ThrowIfNull(resource);

        var resourceKind = ResourceDescriptorRegistry.ResolveResourceKind(typeof(TResource));
        var actionName = ResourceDescriptorRegistry.ToActionString(action);

        return definition.WithPermission(resourceKind, actionName, resourceId, resourceAttributes);
    }

    public static LinkDefinition RequirePermission(
        this LinkDefinition definition,
        PermissionAction action,
        Type resourceType,
        string? resourceId = null,
        IReadOnlyDictionary<string, object>? resourceAttributes = null)
    {
        var resourceKind = ResourceDescriptorRegistry.ResolveResourceKind(resourceType);
        var actionName = ResourceDescriptorRegistry.ToActionString(action);

        return definition.WithPermission(resourceKind, actionName, resourceId, resourceAttributes);
    }
}
