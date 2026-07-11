// ABOUTME: Browser interop contract for explicit Web Push permission, subscription, and opt-out actions.
// ABOUTME: Keeps Push API key material inside the browser-to-BFF enrollment flow without exposing access tokens.

namespace Explore.Blazor.Client.Contracts.Interop;

public interface IWebPushBrowserInterop : IAsyncDisposable
{
    Task<WebPushBrowserState> GetStateAsync(CancellationToken cancellationToken = default);

    Task<WebPushBrowserSubscription?> SubscribeAsync(
        string applicationServerKey,
        CancellationToken cancellationToken = default);

    Task<bool> UnsubscribeAsync(CancellationToken cancellationToken = default);
}
