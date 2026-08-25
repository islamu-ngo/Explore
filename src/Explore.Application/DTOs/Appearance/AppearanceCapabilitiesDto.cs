// ABOUTME: DTO describing what the current user can do with the appearance subsystem.
// ABOUTME: The UI uses this to show/hide actions like clone, edit, create, and manage.

namespace Explore.Application.DTOs.Appearance;

public sealed record AppearanceCapabilitiesDto
{
    public bool CanEditProfile { get; init; }
    public bool CanCreateCustomProfile { get; init; }
    public bool CanClonePreset { get; init; }
    public bool CanDeleteProfile { get; init; }
}
