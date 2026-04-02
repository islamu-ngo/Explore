// ABOUTME: Observable state service for shell-level AI assistant rail open/close and availability.
// ABOUTME: Scoped per circuit — availability is loaded once from public experience settings.

namespace Explore.Blazor.Client.Services;

public sealed class AiAssistantState
{
    public bool IsOpen { get; private set; }
    public bool IsAvailable { get; private set; }
    public event Action? OnChange;

    /// <summary>
    /// Sets whether AI assistant is available for the current tenant.
    /// Called once from MainLayout after loading public experience settings.
    /// When availability is revoked, the rail auto-closes.
    /// </summary>
    public void SetAvailable(bool value)
    {
        if (IsAvailable == value) return;
        IsAvailable = value;
        if (!value) IsOpen = false;
        OnChange?.Invoke();
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
}
