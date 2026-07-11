// ABOUTME: Strong identifier for dock panels registered by shell or workspace components.
// ABOUTME: Keeps dock panel identity stable without requiring a central enum for every panel.

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockPanelId
{
    public DockPanelId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
