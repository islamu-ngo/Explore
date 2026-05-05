// ABOUTME: Classifies why the dock layout state changed so hosts can decide whether to persist.
// ABOUTME: Keeps responsive viewport projections from being saved as durable user preferences.

namespace Explore.Blazor.Client.Services.Docking;

public enum DockLayoutChangeReason
{
    None = 0,
    Registration,
    UserAction,
    ViewportPolicy,
    SnapshotRestore,
    Reset,
    Refresh
}
