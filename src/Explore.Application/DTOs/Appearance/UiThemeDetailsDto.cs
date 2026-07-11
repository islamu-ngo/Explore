// ABOUTME: Detail DTO for admin editing of a single UI theme.
// ABOUTME: Includes both bounded palettes plus the row-version token required for deterministic updates.

namespace Explore.Application.DTOs.Appearance;

public class UiThemeDetailsDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPlatformTheme { get; set; }
    public int SortOrder { get; set; }
    public uint RowVersion { get; set; }
    public UiThemePaletteDto LightPalette { get; set; } = new();
    public UiThemePaletteDto DarkPalette { get; set; } = new();
}
