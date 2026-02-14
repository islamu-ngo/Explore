// ABOUTME: Metadata attribute for marking MediatR commands with resource/action authorization requirements.
// ABOUTME: Used by AuthorizationBehavior as an alternative to implementing IAuthorizedRequest.

namespace Explore.Application.Authorization;

/// <summary>
/// Marks a MediatR command with authorization requirements.
/// The AuthorizationBehavior pipeline behavior reads this attribute to determine
/// the resource kind and action for the authorization check.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuthorizeResourceAttribute : Attribute
{
    /// <summary>
    /// The resource kind (e.g., "instance_setting", "tenant_setting").
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// The action being performed (e.g., "update", "delete").
    /// </summary>
    public string Action { get; }

    public AuthorizeResourceAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }

    /// <summary>
    /// Typed constructor using <see cref="PermissionAction"/> enum instead of raw strings.
    /// Delegates to <see cref="ResourceDescriptorRegistry.ToActionString"/> for conversion.
    /// </summary>
    public AuthorizeResourceAttribute(string resource, PermissionAction action)
    {
        Resource = resource;
        Action = ResourceDescriptorRegistry.ToActionString(action);
    }
}
