// ABOUTME: Detail DTO for admin editing of a single UI theme.
// ABOUTME: Includes both bounded palettes plus the row-version token required for deterministic updates.

namespace Explore.Application.DTOs.Appearance;

public sealed record UiThemeDetailsDto
{
    public Guid Id { get; init; }
    public string ThemeKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public bool IsPlatformTheme { get; init; }
    public int SortOrder { get; init; }
    public uint RowVersion { get; init; }
    public UiThemePaletteDto LightPalette { get; init; } = new();
    public UiThemePaletteDto DarkPalette { get; init; } = new();
}
