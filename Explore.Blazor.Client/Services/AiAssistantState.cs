// ABOUTME: Observable state service for shell-level AI assistant rail open/close and effective availability.
// ABOUTME: Combines tenant policy, viewer authentication state, and user navbar preference.

namespace Explore.Blazor.Client.Services;

public sealed class AiAssistantState
{
    private bool _tenantAvailable;
    private bool _allowAnonymousAccess;
    private bool _isAuthenticated;
    private bool _showNavbarButton = true;

    public bool IsOpen { get; private set; }
    public bool IsAvailable { get; private set; }
    public event Action? OnChange;

    /// <summary>
    /// Applies tenant policy and current viewer context. The effective availability remains
    /// constrained by the user's personal navbar preference.
    /// </summary>
    public void SetPolicy(bool tenantAvailable, bool allowAnonymousAccess, bool isAuthenticated)
    {
        _tenantAvailable = tenantAvailable;
        _allowAnonymousAccess = allowAnonymousAccess;
        _isAuthenticated = isAuthenticated;
        RecomputeAvailability();
    }

    public void SetUserNavbarPreference(bool showNavbarButton)
    {
        _showNavbarButton = showNavbarButton;
        RecomputeAvailability();
    }

    public void Toggle()
    {
        if (!IsAvailable) return;
        IsOpen = !IsOpen;
        OnChange?.Invoke();
    }

    public void Open()
    {
        if (!IsAvailable || IsOpen) return;
        IsOpen = true;
        OnChange?.Invoke();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        OnChange?.Invoke();
    }

    private void RecomputeAvailability()
    {
        var value = _tenantAvailable
            && (_isAuthenticated || _allowAnonymousAccess)
            && _showNavbarButton;

        if (IsAvailable == value)
        {
            return;
        }

        IsAvailable = value;
        if (!value)
        {
            IsOpen = false;
        }

        OnChange?.Invoke();
    }
}
