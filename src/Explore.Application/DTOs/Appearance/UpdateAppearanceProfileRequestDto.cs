// ABOUTME: Grouped PATCH contract for a user-owned appearance profile's editable metadata and palettes.
// ABOUTME: Route identity is authoritative and omitted groups preserve persisted profile values.

namespace Explore.Application.DTOs.Appearance;

public sealed record UpdateAppearanceProfileRequestDto
{
    public UpdateAppearanceProfileMetadataDto? Metadata { get; init; }
    public UpdateAppearanceProfilePalettesDto? Palettes { get; init; }
}

public sealed record UpdateAppearanceProfileMetadataDto
{
    public string? Name { get; init; }
    public string? ThemeMode { get; init; }
}

public sealed record UpdateAppearanceProfilePalettesDto
{
    public UiThemePaletteDto? Light { get; init; }
    public UiThemePaletteDto? Dark { get; init; }
}
