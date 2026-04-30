// ABOUTME: Immutable metadata that describes how a dock panel should be hosted.
// ABOUTME: Separates panel capabilities from mutable open, active, width, and ordering state.

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockPanelDescriptor(
    DockPanelId Id,
    DockScope Scope,
    DockSide Side,
    DockMode DefaultMode,
    string Title,
    string AriaLabel,
    int DefaultWidth,
    int MinWidth,
    int MaxWidth,
    int Order,
    bool IsResizable,
    bool CanClose,
    bool PersistState)
{
    public DockPanelDescriptor Validate()
    {
        ArgumentNullException.ThrowIfNull(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(AriaLabel);

        if (MinWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MinWidth), "Minimum width must be greater than zero.");
        }

        if (MaxWidth < MinWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxWidth), "Maximum width must be greater than or equal to minimum width.");
        }

        if (DefaultWidth < MinWidth || DefaultWidth > MaxWidth)
        {
            throw new ArgumentOutOfRangeException(nameof(DefaultWidth), "Default width must be between minimum and maximum width.");
        }

        return this;
    }
}
