// ABOUTME: Code-behind for AppWorkspaceRail — permanent shell chrome consuming workspace registry and shell state.
// ABOUTME: Filters authenticated workspaces via AuthenticationStateProvider without role or claim checks.

using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Services.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Components.Shell;

public partial class AppWorkspaceRail : ComponentBase, IDisposable
{
    [Inject]
    private IWorkspaceRegistry Registry { get; set; } = null!;

    [Inject]
    private UiShellState ShellState { get; set; } = null!;

    [Inject]
    private AuthenticationStateProvider AuthProvider { get; set; } = null!;

    [Inject]
    private ITranslationService TranslationService { get; set; } = null!;

    private IReadOnlyList<WorkspaceDescriptor> _visibleWorkspaces = [];
    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        ShellState.Changed += OnShellStateChanged;
        await RefreshVisibleWorkspacesAsync();
    }

    private async Task RefreshVisibleWorkspacesAsync()
    {
        var isAuthenticated = await IsUserAuthenticatedAsync();

        _visibleWorkspaces = Registry.Workspaces
            .Where(workspace => !workspace.RequiresAuthentication || isAuthenticated)
            .OrderBy(workspace => workspace.Key == WorkspaceKey.Settings ? 1 : 0)
            .ThenBy(workspace => workspace.LabelKey, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<bool> IsUserAuthenticatedAsync()
    {
        try
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated == true;
        }
        catch
        {
            return false;
        }
    }

    private void OnShellStateChanged() => _ = InvokeAsync(StateHasChanged);

    private bool IsActive(WorkspaceDescriptor workspace) => ShellState.ActiveWorkspace == workspace.Key;

    private string GetRoute(WorkspaceDescriptor workspace) =>
        ShellState.GetLastRoute(workspace.Key) ?? workspace.BaseRoute;

    private string GetLabel(WorkspaceDescriptor workspace)
    {
        var fallback = workspace.Key switch
        {
            _ when workspace.Key == WorkspaceKey.Events => "Events",
            _ when workspace.Key == WorkspaceKey.Settings => "Settings",
            _ => workspace.LabelKey
        };

        return TranslationService.T(workspace.LabelKey, fallback);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ShellState.Changed -= OnShellStateChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
