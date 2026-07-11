// ABOUTME: DTO carrying the bounded palette tokens used to create or update a UI theme.
// ABOUTME: Mirrors the domain palette structure while keeping transport and validation in the application layer.

namespace Explore.Application.DTOs.Appearance;

public class UiThemePaletteDto
{
    public string Primary { get; set; } = string.Empty;
    public string PrimaryContrastText { get; set; } = "#FFFFFF";
    public string Secondary { get; set; } = string.Empty;
    public string SecondaryContrastText { get; set; } = "#FFFFFF";
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
