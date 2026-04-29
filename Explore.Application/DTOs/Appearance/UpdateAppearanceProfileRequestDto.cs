// ABOUTME: Input DTO for updating a user-owned appearance profile's palette colors or metadata.
// ABOUTME: Only palettes and name are editable; lineage fields are read-only.

namespace Explore.Application.DTOs.Appearance;

public sealed class UpdateAppearanceProfileRequestDto
{
    public string? Name { get; set; }
    public UiThemePaletteDto? LightPaletteSnapshot { get; set; }
    public UiThemePaletteDto? DarkPaletteSnapshot { get; set; }
    public string? ThemeMode { get; set; }
}