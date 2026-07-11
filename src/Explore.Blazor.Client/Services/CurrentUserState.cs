// ABOUTME: Observable state service for cross-component current-user communication.
// ABOUTME: Notifies NavMenu (and other subscribers) when the user profile is updated so the navbar avatar refreshes live.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public sealed class CurrentUserState
{
    public UserDto? Current { get; private set; }
    public event Action? OnChanged;

    public void NotifyUpdated(UserDto user)
    {
        Current = user;
        OnChanged?.Invoke();
    }
}
