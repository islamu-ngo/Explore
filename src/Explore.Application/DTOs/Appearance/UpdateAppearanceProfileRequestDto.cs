// ABOUTME: Grouped PATCH contract for a user-owned appearance profile's editable metadata and palettes.
// ABOUTME: Route identity is authoritative and omitted groups preserve persisted profile values.

namespace Explore.Application.DTOs.Appearance;

public sealed class UpdateAppearanceProfileRequestDto
{
    public UpdateAppearanceProfileMetadataDto? Metadata { get; set; }
    public UpdateAppearanceProfilePalettesDto? Palettes { get; set; }
}

public sealed class UpdateAppearanceProfileMetadataDto
{
    public string? Name { get; set; }
    public string? ThemeMode { get; set; }
}

public sealed class UpdateAppearanceProfilePalettesDto
{
    public UiThemePaletteDto? Light { get; set; }
    public UiThemePaletteDto? Dark { get; set; }
}
