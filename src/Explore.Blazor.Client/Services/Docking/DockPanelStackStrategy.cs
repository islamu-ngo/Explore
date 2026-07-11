// ABOUTME: Defines how multiple open docked panels on the same side share visible space.
// ABOUTME: Keeps stack behavior explicit on descriptors instead of hard-coding it by side.

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockPanelStackStrategy
{
    public static readonly DockPanelStackStrategy Tabbed = new("Tabbed");
    public static readonly DockPanelStackStrategy Split = new("Split");

    private DockPanelStackStrategy(string value) => Value = value;

    public string Value { get; }

    public override string ToString() => Value;
}
