// ABOUTME: Defines how an open docked panel is projected when a dock host enters mobile layout.
// ABOUTME: Separates durable open intent from the mobile presentation shell used by renderers.

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockPanelMobilePresentation
{
    public static readonly DockPanelMobilePresentation TemporaryOverlay = new("TemporaryOverlay");
    public static readonly DockPanelMobilePresentation FullscreenOverlay = new("FullscreenOverlay");

    private DockPanelMobilePresentation(string value) => Value = value;

    public string Value { get; }

    public override string ToString() => Value;
}
