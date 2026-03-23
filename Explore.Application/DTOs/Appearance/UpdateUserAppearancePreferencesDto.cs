// ABOUTME: Input DTO for updating user-scoped appearance preferences.
// ABOUTME: Currently limited to theme mode so the persistence contract stays narrow and easy to evolve.

namespace Explore.Application.DTOs.Appearance;

public class UpdateUserAppearancePreferencesDto
{
    public string ThemeMode { get; set; } = "system";
}
