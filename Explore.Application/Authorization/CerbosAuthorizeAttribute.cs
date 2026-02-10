// ABOUTME: Metadata attribute for marking MediatR commands with Cerbos resource/action requirements.
// Used by AuthorizationBehavior as an alternative to implementing IAuthorizedRequest.

namespace Explore.Application.Authorization;

/// <summary>
/// Marks a MediatR command with Cerbos authorization requirements.
/// The AuthorizationBehavior pipeline behavior reads this attribute to determine
/// the resource kind and action for the Cerbos policy check.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CerbosAuthorizeAttribute : Attribute
{
    /// <summary>
    /// The Cerbos resource kind (e.g., "instance_setting", "tenant_setting").
    /// </summary>
    public string Resource { get; }

    /// <summary>
    /// The action being performed (e.g., "update", "delete").
    /// </summary>
    public string Action { get; }

    public CerbosAuthorizeAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }
}
