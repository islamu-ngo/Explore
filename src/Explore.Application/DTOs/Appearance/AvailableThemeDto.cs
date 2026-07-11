// ABOUTME: Runtime DTO representing a selectable theme with full light/dark palettes for immediate MudBlazor rendering.
// ABOUTME: Returned by the authenticated theme-picker endpoint so the client can preview and apply themes without extra round trips.

namespace Explore.Application.DTOs.Appearance;

public class AvailableThemeDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPlatformTheme { get; set; }
    public int SortOrder { get; set; }
    public required UiThemePaletteDto LightPalette { get; set; }
    public required UiThemePaletteDto DarkPalette { get; set; }
}
