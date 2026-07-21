// ABOUTME: Compile-time catalog of workspaces currently available to the application shell.
// ABOUTME: Starts with Events and Settings; later phases add Studio and AI descriptors.

namespace Explore.Blazor.Client.Services.Shell;

using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Components.Shell.Workspaces;
using MudBlazor;

public sealed class WorkspaceRegistry : IWorkspaceRegistry
{
    private static readonly IReadOnlyList<WorkspaceDescriptor> RegisteredWorkspaces =
    [
        new(
            WorkspaceKey.Events,
            "workspace.events",
            Icons.Material.Filled.Explore,
            "/",
            RequiresAuthentication: false,
            NavigationProviderType: typeof(EventsWorkspaceNavigation)),
        new(
            WorkspaceKey.Settings,
            "workspace.settings",
            Icons.Material.Filled.Settings,
            "/settings",
            RequiresAuthentication: true,
            NavigationProviderType: typeof(SettingsWorkspaceNavigation))
    ];

    public IReadOnlyList<WorkspaceDescriptor> Workspaces => RegisteredWorkspaces;
}