// ABOUTME: Grouped PATCH contract for updating an existing UI theme with optimistic concurrency.
// ABOUTME: Route identity is authoritative and omitted metadata, state, or palette groups preserve persisted values.

namespace Explore.Application.DTOs.Appearance;

using Explore.Application.Models.Common;

public class UpdateUiThemeDto
{
    public required uint RowVersion { get; set; }
    public UpdateUiThemeMetadataDto? Metadata { get; set; }
    public UpdateUiThemeStateDto? State { get; set; }
    public UpdateUiThemePalettesDto? Palettes { get; set; }
}

public sealed class UpdateUiThemeMetadataDto
{
    public string? ThemeKey { get; set; }
    public string? DisplayName { get; set; }
    public OptionalUpdate<string> Description { get; set; } = OptionalUpdate<string>.Unspecified();
}

public sealed class UpdateUiThemeStateDto
{
    public bool? IsActive { get; set; }
    public bool? IsDefault { get; set; }
    public int? SortOrder { get; set; }
}

public sealed class UpdateUiThemePalettesDto
{
    public UiThemePaletteDto? Light { get; set; }
    public UiThemePaletteDto? Dark { get; set; }
}
