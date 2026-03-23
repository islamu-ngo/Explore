// ABOUTME: Input model for updating an existing UI theme with optimistic concurrency.
// ABOUTME: Carries the row-version observed by the client so stale edits can be rejected deterministically.

namespace Explore.Application.DTOs.Appearance;

public class UpdateUiThemeDto
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public uint RowVersion { get; set; }
    public UiThemePaletteDto LightPalette { get; set; } = new();
    public UiThemePaletteDto DarkPalette { get; set; } = new();
}
