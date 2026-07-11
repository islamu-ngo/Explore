// ABOUTME: DTO describing what the current user can do with the appearance subsystem.
// ABOUTME: The UI uses this to show/hide actions like clone, edit, create, and manage.

namespace Explore.Application.DTOs.Appearance;

public sealed class AppearanceCapabilitiesDto
{
    public bool CanEditProfile { get; set; }
    public bool CanCreateCustomProfile { get; set; }
    public bool CanClonePreset { get; set; }
    public bool CanDeleteProfile { get; set; }
}
