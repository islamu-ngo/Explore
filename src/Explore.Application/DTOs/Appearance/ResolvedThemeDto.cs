// ABOUTME: DTO carrying the rendered theme data (name, palettes, editability, origin) within a resolved appearance.
// ABOUTME: The UI uses IsSnapshot and IsUserEditable to decide which actions are available.

namespace Explore.Application.DTOs.Appearance;

public sealed class ResolvedThemeDto
{
    public string DisplayName { get; set; } = default!;
    public required UiThemePaletteDto LightPalette { get; set; }
    public required UiThemePaletteDto DarkPalette { get; set; }

    /// <summary>True if this theme data was snapshotted from a user profile rather than resolved from a live preset.</summary>
    public bool IsSnapshot { get; set; }

    /// <summary>True if the current user can edit the palette colors directly.</summary>
    public bool IsUserEditable { get; set; }

    /// <summary>Origin provenance: SystemPreset, TenantPreset, UserCustom, or Fallback.</summary>
    public string? Origin { get; set; }
}
