// ABOUTME: Input DTO for setting just the theme mode (light/dark/system) without changing the active profile.
// ABOUTME: Allows the quick switcher to toggle mode independently.

namespace Explore.Application.DTOs.Appearance;

public sealed class SetThemeModeRequestDto
{
    public string ThemeMode { get; set; } = "system";
}