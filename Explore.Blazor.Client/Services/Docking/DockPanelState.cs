// ABOUTME: Mutable dock panel runtime state represented as an immutable value object.
// ABOUTME: Captures open mode, width, ordering, and active state independently from descriptors.

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockPanelState(
    DockPanelId Id,
    bool IsOpen,
    DockMode Mode,
    int Width,
    int Order,
    bool IsActive);
