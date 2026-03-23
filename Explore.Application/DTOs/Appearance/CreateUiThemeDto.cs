// ABOUTME: Input model for creating a new platform or tenant-owned UI theme.
// ABOUTME: Keeps scope intent explicit while the handler derives the actual owner scope from admin authorization.

namespace Explore.Application.DTOs.Appearance;

public class CreateUiThemeDto
{
    public bool IsPlatformTheme { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public UiThemePaletteDto LightPalette { get; set; } = new();
    public UiThemePaletteDto DarkPalette { get; set; } = new();
}
