// ABOUTME: Input DTO for setting just the theme mode (light/dark/system) without changing the active profile.
// ABOUTME: Allows the quick switcher to toggle mode independently.

namespace Explore.Application.DTOs.Appearance;

public sealed record SetThemeModeRequestDto
{
    public string ThemeMode { get; init; } = "system";
}
