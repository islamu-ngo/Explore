// ABOUTME: Lightweight service for cross-component consent banner communication.
// ABOUTME: Footer triggers OnReopenRequested; AnalyticsInitializer subscribes to reopen the banner.

namespace Explore.Blazor.Client.Services;

public sealed class CookieConsentStateService
{
    public event Func<Task>? OnReopenRequested;

    public async Task RequestReopenAsync()
    {
        if (OnReopenRequested is not null)
        {
            await OnReopenRequested.Invoke();
        }
    }
}
