namespace Explore.Application.Hateoas;

using System.Collections.Generic;
using Explore.Application.Authorization;

/// <summary>
/// Defines a link that can be generated for a resource.
/// Used by link policies to specify which links should be included for a resource.
/// </summary>
/// <param name="Rel">The link relation type (e.g., "self", "collection", "events").</param>
/// <param name="RouteName">The name of the route to generate the URL.</param>
/// <param name="RouteValues">Optional route values for URL generation.</param>
/// <param name="Method">Optional HTTP method (GET, POST, PUT, DELETE). Null implies GET.</param>
/// <param name="Title">Optional human-readable title for the link.</param>
/// <param name="RequiresAuth">Whether the link requires authentication to be included.</param>
/// <param name="RequiredRoles">Roles required to include this link (null = any authenticated user).</param>
/// <param name="Condition">Optional condition that must be true for the link to be included.</param>
/// <param name="PermissionResourceKind">Optional resource kind used for action-level authorization.</param>
/// <param name="PermissionAction">Optional permission action used for action-level authorization.</param>
/// <param name="PermissionResourceId">Optional explicit resource identifier for permission checks.</param>
/// <param name="PermissionResourceAttributes">Optional resource attributes passed to authorization provider.</param>
/// <param name="PermissionScope">Optional tenant/org authorization scope passed to authorization provider.</param>
public sealed record LinkDefinition(
    string Rel,
    string RouteName,
    object? RouteValues = null,
    string? Method = null,
    string? Title = null,
    bool RequiresAuth = false,
    string[]? RequiredRoles = null,
    Func<bool>? Condition = null,
    string? PermissionResourceKind = null,
    string? PermissionAction = null,
    string? PermissionResourceId = null,
    IReadOnlyDictionary<string, object>? PermissionResourceAttributes = null,
    AuthorizationScope? PermissionScope = null)
{
    /// <summary>
    /// Creates a self link definition.
    /// </summary>
    public static LinkDefinition Self(string routeName, object? routeValues = null) =>
        new(LinkRelations.Self, routeName, routeValues);

    /// <summary>
    /// Creates a collection link definition.
    /// </summary>
    public static LinkDefinition Collection(string routeName, object? routeValues = null) =>
        new(LinkRelations.Collection, routeName, routeValues);

    /// <summary>
    /// Creates an edit/update link definition (requires authentication).
    /// </summary>
    public static LinkDefinition Edit(string routeName, object? routeValues = null, string[]? roles = null) =>
        new(LinkRelations.Edit, routeName, routeValues, "PUT", RequiresAuth: true, RequiredRoles: roles);

    /// <summary>
    /// Creates a delete link definition (requires authentication).
    /// </summary>
    public static LinkDefinition Delete(string routeName, object? routeValues = null, string[]? roles = null) =>
        new(LinkRelations.Delete, routeName, routeValues, "DELETE", RequiresAuth: true, RequiredRoles: roles);

    /// <summary>
    /// Creates a create link definition (requires authentication).
    /// </summary>
    public static LinkDefinition Create(string routeName, object? routeValues = null, string[]? roles = null) =>
        new(LinkRelations.Create, routeName, routeValues, "POST", RequiresAuth: true, RequiredRoles: roles);

    /// <summary>
    /// Creates a related resource link definition.
    /// </summary>
    public static LinkDefinition Related(string rel, string routeName, object? routeValues = null) =>
        new(rel, routeName, routeValues);

    /// <summary>
    /// Creates an action link definition with method.
    /// </summary>
    public static LinkDefinition Action(string rel, string routeName, string method, object? routeValues = null, bool requiresAuth = true) =>
        new(rel, routeName, routeValues, method, RequiresAuth: requiresAuth);

    /// <summary>
    /// Creates a conditional link that only appears when the condition is met.
    /// </summary>
    public LinkDefinition When(Func<bool> condition) =>
        this with { Condition = condition };

    /// <summary>
    /// Specifies that this link requires authentication.
    /// </summary>
    public LinkDefinition Authenticated() =>
        this with { RequiresAuth = true };

    /// <summary>
    /// Specifies roles required for this link.
    /// </summary>
    public LinkDefinition WithRoles(params string[] roles) =>
        this with { RequiresAuth = true, RequiredRoles = roles };

    /// <summary>
    /// Specifies resource/action metadata for link-level authorization checks.
    /// </summary>
    public LinkDefinition WithPermission(
        string resourceKind,
        string action,
        string? resourceId = null,
        IReadOnlyDictionary<string, object>? resourceAttributes = null,
        AuthorizationScope? scope = null) =>
        this with
        {
            PermissionResourceKind = resourceKind,
            PermissionAction = action,
            PermissionResourceId = resourceId,
            PermissionResourceAttributes = resourceAttributes,
            PermissionScope = scope
        };
}
