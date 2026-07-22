// ABOUTME: Code-behind for AppWorkspaceRail — permanent shell chrome consuming workspace registry and shell state.
// ABOUTME: Filters authenticated workspaces via AuthenticationStateProvider and server-gated availability from UiShellContextService.

using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Contracts.Services.Shell;
using Explore.Blazor.Client.Services;
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
    private IUiShellContextService ShellContextService { get; set; } = null!;

    [Inject]
    private CurrentUserState CurrentUserState { get; set; } = null!;

    [Inject]
    private ITranslationService TranslationService { get; set; } = null!;

    private IReadOnlyList<WorkspaceDescriptor> _visibleWorkspaces = [];
    private IReadOnlyList<SettingsLink> _settingsLinks = [];
    private bool _showSiteAdministration;
    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        ShellState.Changed += OnShellStateChanged;
        CurrentUserState.OnChanged += OnCurrentUserChanged;
        await RefreshVisibleWorkspacesAsync();
    }

    private async Task RefreshVisibleWorkspacesAsync()
    {
        var isAuthenticated = await IsUserAuthenticatedAsync();

        Explore.Blazor.Client.Clients.UiShellContextDto? context = null;
        if (isAuthenticated)
        {
            context = await ShellContextService.GetCachedContextAsync();
        }

        var availability = context?.Workspaces;
        var scopes = context?.SettingsScopes ?? [];
        var isSingleTenant = string.IsNullOrWhiteSpace(context?.DeploymentMode)
            || string.Equals(context.DeploymentMode, "SingleTenant", StringComparison.OrdinalIgnoreCase);
        var hasTenant = scopes.Any(scope => IsScope(scope, "Tenant"));
        var hasInstance = scopes.Any(scope => IsScope(scope, "Instance"));
        _showSiteAdministration = isSingleTenant && hasTenant && hasInstance;
        _settingsLinks = scopes
            .Select(scope => CreateSettingsLink(scope, _showSiteAdministration))
            .Where(link => link is not null)
            .Select(link => link!)
            .ToList();

        _visibleWorkspaces = Registry.Workspaces
            .Where(workspace => IsWorkspaceVisible(workspace, isAuthenticated, availability))
            .ToList();

        ShellState.ReconcileAvailability(key =>
            Registry.Workspaces.FirstOrDefault(w => w.Key == key) is { } descriptor
            && IsWorkspaceVisible(descriptor, isAuthenticated, availability));
    }

    private static bool IsWorkspaceVisible(
        WorkspaceDescriptor workspace,
        bool isAuthenticated,
        Explore.Blazor.Client.Clients.WorkspaceAvailabilityDto? availability)
    {
        if (workspace.RequiresAuthentication && !isAuthenticated)
        {
            return false;
        }

        if (workspace.AvailabilityPolicy is not null && !workspace.AvailabilityPolicy(availability))
        {
            return false;
        }

        return true;
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

    private void OnCurrentUserChanged() => _ = InvokeAsync(async () =>
    {
        await RefreshVisibleWorkspacesAsync();
        await InvokeAsync(StateHasChanged);
    });

    private bool IsActive(WorkspaceDescriptor workspace) => ShellState.ActiveWorkspace == workspace.Key;

    private bool IsUtilityOpen(WorkspaceDescriptor workspace) =>
        workspace.Key == WorkspaceKey.Settings && ShellState.IsPersonalSettingsOpen;

    private static string GetLinkClass(bool isActive, bool isUtilityOpen) =>
        $"app-workspace-rail__link{(isActive ? " app-workspace-rail__link--active" : string.Empty)}{(isUtilityOpen ? " app-workspace-rail__link--utility-open" : string.Empty)}";

    private string GetRoute(WorkspaceDescriptor workspace) => workspace.Key == WorkspaceKey.Settings
        ? "/settings/personal"
        : ShellState.GetLastRoute(workspace.Key) ?? workspace.BaseRoute;

    private void OnWorkspaceClicked(WorkspaceDescriptor workspace, Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
    {
        if (workspace.Key == WorkspaceKey.Settings)
        {
            ShellState.NavigateToPersonalSettings(GetRoute(workspace), args);
        }
    }

    private string GetLabel(WorkspaceDescriptor workspace)
    {
        var fallback = workspace.Key switch
        {
            _ when workspace.Key == WorkspaceKey.Events => "Events",
            _ when workspace.Key == WorkspaceKey.Settings => "Settings",
            _ when workspace.Key == WorkspaceKey.Studio => "Studio",
            _ when workspace.Key == WorkspaceKey.Ai => "AI",
            _ => workspace.LabelKey
        };

        return TranslationService.T(workspace.LabelKey, fallback);
    }

    private static SettingsLink? CreateSettingsLink(
        Explore.Blazor.Client.Clients.SettingsScopeDto scope,
        bool siteAdministration)
    {
        var kind = scope.Scope?.Trim();
        return kind?.ToUpperInvariant() switch
        {
            "ORGANIZATION" when scope.ScopeId.HasValue => new($"/settings/organization/{scope.ScopeId}", scope.DisplayName ?? "Organization"),
            "GROUP" when scope.ScopeId.HasValue => new($"/settings/group/{scope.ScopeId}", scope.DisplayName ?? "Group"),
            "TENANT" => new("/settings/tenant", siteAdministration ? "Tenant settings" : "Tenant administration"),
            "INSTANCE" => new("/settings/instance", siteAdministration ? "Instance settings" : "Instance administration"),
            _ => null
        };
    }

    private static bool IsScope(Explore.Blazor.Client.Clients.SettingsScopeDto scope, string kind) =>
        string.Equals(scope.Scope, kind, StringComparison.OrdinalIgnoreCase);

    private sealed record SettingsLink(string Href, string Label);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ShellState.Changed -= OnShellStateChanged;
        CurrentUserState.OnChanged -= OnCurrentUserChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
