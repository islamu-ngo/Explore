// ABOUTME: Enum representing standard policy actions (read, create, update, delete).
// ABOUTME: Used by ResourceDescriptorRegistry and HATEOAS RequirePermission extensions for typed action references.

namespace Explore.Application.Authorization;

/// <summary>
/// Standard policy actions used across the application.
/// Mapped to lowercase string values for authorization checks.
/// </summary>
public enum PermissionAction
{
    Read,
    Create,
    Update,
    Delete
}
