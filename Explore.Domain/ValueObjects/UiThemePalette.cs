// ABOUTME: Bounded theme palette value object covering only MudBlazor tokens currently used by the layouts.
// ABOUTME: Mapped as explicit owned columns for light and dark palettes instead of JSON blobs.

namespace Explore.Domain.ValueObjects;

public class UiThemePalette
{
    public required string Primary { get; set; }
    public required string Secondary { get; set; }
    public required string Background { get; set; }
    public required string Surface { get; set; }
    public required string AppbarBackground { get; set; }
    public required string AppbarText { get; set; }
    public required string DrawerBackground { get; set; }
    public required string DrawerText { get; set; }
    public required string DrawerIcon { get; set; }
    public required string TextPrimary { get; set; }
    public required string TextSecondary { get; set; }
    public required string Info { get; set; }
    public required string Success { get; set; }
    public required string Warning { get; set; }
    public required string Error { get; set; }
    public required string LinesDefault { get; set; }
    public required string Divider { get; set; }
}
