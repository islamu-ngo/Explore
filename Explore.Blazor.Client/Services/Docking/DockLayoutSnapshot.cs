// ABOUTME: Serializable snapshot of dock panel runtime state for reset, persistence, and tests.
// ABOUTME: Stores state by stable panel ids without taking dependencies on shell or EventList details.

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockLayoutSnapshot(
    string LayoutKey,
    IReadOnlyList<DockPanelState> Panels,
    DateTimeOffset UpdatedAt);
