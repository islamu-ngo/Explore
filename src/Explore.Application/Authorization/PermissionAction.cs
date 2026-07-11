// ABOUTME: Legacy enum for policy actions. Superseded by AuthorizationActions string constants.
// ABOUTME: Marked obsolete — migrate callers to use AuthorizationActions constants directly.

namespace Explore.Application.Authorization;

/// <summary>
/// Legacy policy action enum. Use <see cref="AuthorizationActions"/> string constants instead.
/// </summary>
[Obsolete("Use AuthorizationActions string constants instead. This enum will be removed in a future release.")]
public enum PermissionAction
{
    Read,
    Create,
    Update,
    Delete,
    ManageMembers,
    ViewSharedContacts,
    ExportSharedContacts
}
