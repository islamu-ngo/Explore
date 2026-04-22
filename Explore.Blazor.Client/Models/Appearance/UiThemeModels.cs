// ABOUTME: Client-side mirrors of UI theme admin DTOs used by catalog section and editor dialog.
// ABOUTME: Matches server shapes under Explore.Application.DTOs.Appearance for JSON wire compatibility.

namespace Explore.Blazor.Client.Models.Appearance;

public sealed class UiThemePaletteModel
{
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string AppbarBackground { get; set; } = string.Empty;
    public string AppbarText { get; set; } = string.Empty;
    public string DrawerBackground { get; set; } = string.Empty;
    public string DrawerText { get; set; } = string.Empty;
    public string DrawerIcon { get; set; } = string.Empty;
    public string TextPrimary { get; set; } = string.Empty;
    public string TextSecondary { get; set; } = string.Empty;
    public string Info { get; set; } = string.Empty;
    public string Success { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string LinesDefault { get; set; } = string.Empty;
    public string Divider { get; set; } = string.Empty;
}

public sealed class UiThemeListItemModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public UiThemePaletteModel LightPalette { get; set; } = new();
    public UiThemePaletteModel DarkPalette { get; set; } = new();
}

public sealed class UiThemeDetailsModel
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public UiThemePaletteModel LightPalette { get; set; } = new();
    public UiThemePaletteModel DarkPalette { get; set; } = new();
    public long RowVersion { get; set; }
}

public sealed class CreateUiThemeModel
{
    public bool IsPlatformTheme { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public UiThemePaletteModel LightPalette { get; set; } = new();
    public UiThemePaletteModel DarkPalette { get; set; } = new();
}

public sealed class UpdateUiThemeModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public UiThemePaletteModel LightPalette { get; set; } = new();
    public UiThemePaletteModel DarkPalette { get; set; } = new();
    public long RowVersion { get; set; }
}

public sealed class AvailableThemeModel
{
    public Guid Id { get; set; }
    public string ThemeKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public UiThemePaletteModel LightPalette { get; set; } = new();
    public UiThemePaletteModel DarkPalette { get; set; } = new();
}
