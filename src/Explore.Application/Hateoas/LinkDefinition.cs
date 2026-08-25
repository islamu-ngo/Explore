namespace Explore.Application.Hateoas;

using System.Collections.Immutable;
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
/// <param name="PermissionScope">Optional tenant/org authorization scope passed to authorization provider.</param>
/// <param name="PermissionFacts">Closed typed policy facts passed to the authorization provider.</param>
/// <param name="AdvertiseWhenAnonymous">Whether an authenticated link may still be advertised to anonymous clients for login-intercept flows.</param>
public sealed record LinkDefinition(
    string Rel,
    string RouteName,
    object? RouteValues = null,
    string? Method = null,
    string? Title = null,
    bool RequiresAuth = false,
    ImmutableList<string>? RequiredRoles = null,
    Func<bool>? Condition = null,
    string? PermissionResourceKind = null,
    string? PermissionAction = null,
    string? PermissionResourceId = null,
    AuthorizationScope? PermissionScope = null,
    IAuthorizationFacts? PermissionFacts = null,
    bool AdvertiseWhenAnonymous = false)
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
        new(LinkRelations.Edit, routeName, routeValues, "PUT", RequiresAuth: true, RequiredRoles: roles?.ToImmutableList());

    /// <summary>
    /// Creates a delete link definition (requires authentication).
    /// </summary>
    public static LinkDefinition Delete(string routeName, object? routeValues = null, string[]? roles = null) =>
        new(LinkRelations.Delete, routeName, routeValues, "DELETE", RequiresAuth: true, RequiredRoles: roles?.ToImmutableList());

    /// <summary>
    /// Creates a create link definition (requires authentication).
    /// </summary>
    public static LinkDefinition Create(string routeName, object? routeValues = null, string[]? roles = null) =>
        new(LinkRelations.Create, routeName, routeValues, "POST", RequiresAuth: true, RequiredRoles: roles?.ToImmutableList());

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
    /// Keeps an authenticated affordance visible to anonymous clients so the UI can route through sign-in.
    /// Endpoint authorization is unchanged; this only affects HAL visibility.
    /// </summary>
    public LinkDefinition AdvertisedWhenAnonymous() =>
        this with { AdvertiseWhenAnonymous = true };

    /// <summary>
    /// Specifies roles required for this link.
    /// </summary>
    public LinkDefinition WithRoles(params string[] roles) =>
        this with { RequiresAuth = true, RequiredRoles = roles.ToImmutableList() };

    /// <summary>
    /// Specifies resource/action metadata for link-level authorization checks.
    /// </summary>
    public LinkDefinition WithPermission(
        string resourceKind,
        string action,
        string? resourceId = null,
        AuthorizationScope? scope = null,
        IAuthorizationFacts? facts = null) =>
        this with
        {
            PermissionResourceKind = resourceKind,
            PermissionAction = action,
            PermissionResourceId = resourceId,
            PermissionScope = scope,
            PermissionFacts = facts
        };
}
