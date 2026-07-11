// ABOUTME: Defines the layout owner for a dock panel, separating app-shell and page-workspace panels.
// ABOUTME: Prevents workspace panels from leaking into global shell layout decisions.

namespace Explore.Blazor.Client.Services.Docking;

public enum DockScope
{
    Shell,
    Workspace
}
