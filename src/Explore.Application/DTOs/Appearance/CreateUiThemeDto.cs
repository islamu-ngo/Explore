// ABOUTME: Input model for creating a new platform or tenant-owned UI theme.
// ABOUTME: Keeps scope intent explicit while the handler derives the actual owner scope from admin authorization.

namespace Explore.Application.DTOs.Appearance;

public sealed record CreateUiThemeDto
{
    public bool IsPlatformTheme { get; init; }
    public string ThemeKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
    public UiThemePaletteDto LightPalette { get; init; } = new();
    public UiThemePaletteDto DarkPalette { get; init; } = new();
}
