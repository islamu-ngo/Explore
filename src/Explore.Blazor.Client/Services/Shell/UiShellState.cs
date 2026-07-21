// ABOUTME: Scoped route-derived state for the active workspace and session-only route history.
// ABOUTME: Observes Blazor navigation and preserves query strings without durable persistence.

namespace Explore.Blazor.Client.Services.Shell;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

public sealed class UiShellState : IDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly WorkspaceRouteClassifier _classifier;
    private readonly Dictionary<WorkspaceKey, string> _lastRoutes = [];
    private bool _disposed;

    public UiShellState(
        NavigationManager navigationManager,
        WorkspaceRouteClassifier classifier)
    {
        _navigationManager = navigationManager;
        _classifier = classifier;
        ActiveWorkspace = WorkspaceKey.Events;
        UpdateFromLocation(navigationManager.Uri, notify: false);
        navigationManager.LocationChanged += OnLocationChanged;
    }

    public event Action? Changed;

    public WorkspaceKey ActiveWorkspace { get; private set; }

    public string? GetLastRoute(WorkspaceKey workspace) =>
        _lastRoutes.GetValueOrDefault(workspace);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _navigationManager.LocationChanged -= OnLocationChanged;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args) =>
        UpdateFromLocation(args.Location, notify: true);

    private void UpdateFromLocation(string location, bool notify)
    {
        var relative = _navigationManager.ToBaseRelativePath(location);
        var route = string.IsNullOrEmpty(relative) ? "/" : $"/{relative.TrimStart('/')}";
        ActiveWorkspace = _classifier.Classify(route);
        _lastRoutes[ActiveWorkspace] = route;

        if (notify)
        {
            Changed?.Invoke();
        }
    }
}
