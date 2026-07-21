// ABOUTME: Code-behind for the minimal Settings workspace navigation placeholder.
// ABOUTME: Implements IWorkspaceNavigationProvider; replaced with scope-aware nav in Phase 6.

using Explore.Blazor.Client.Contracts.Services.Shell;

namespace Explore.Blazor.Client.Components.Shell.Workspaces;

public partial class SettingsWorkspaceNavigation : IWorkspaceNavigationProvider
{
    public string AriaLabel => "Settings navigation";
}