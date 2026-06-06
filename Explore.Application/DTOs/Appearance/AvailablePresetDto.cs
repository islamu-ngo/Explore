// ABOUTME: DTO for available theme presets (platform + tenant catalogs) separated from user-owned profiles.
// ABOUTME: The quick switcher shows these as selectable templates; clicking one clones it into a user profile.

namespace Explore.Application.DTOs.Appearance;

public sealed class AvailablePresetDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEditable { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public required UiThemePaletteDto LightPalette { get; set; }
    public required UiThemePaletteDto DarkPalette { get; set; }
    public DateTimeOffset? DeprecatedAt { get; set; }
}
