// ABOUTME: Observable state service for cross-component sidebar communication.
// ABOUTME: Allows NavMenu to show a toggle button when a page registers a sidebar.

namespace Explore.Blazor.Client.Services;

public sealed class SidebarState
{
    public bool IsOpen { get; private set; } = true;
    public bool HasSidebar { get; private set; }
    public event Action? OnChange;

    public void SetHasSidebar(bool value)
    {
        var changed = HasSidebar != value;
        HasSidebar = value;
        if (!value)
        {
            IsOpen = false;
        }
        OnChange?.Invoke();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        OnChange?.Invoke();
    }

    public void SetOpen(bool value)
    {
        if (IsOpen == value) return;
        IsOpen = value;
        OnChange?.Invoke();
    }
}
