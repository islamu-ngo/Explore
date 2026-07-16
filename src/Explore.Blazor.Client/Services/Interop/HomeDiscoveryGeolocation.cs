// ABOUTME: JS-module adapter for explicit, low-accuracy browser geolocation on the discovery home page.
// ABOUTME: Fails closed during prerender, denial, timeout, disconnection, or unsupported browser execution.

using Explore.Blazor.Client.Contracts.Interop;
using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services.Interop;

public sealed class HomeDiscoveryGeolocation(
    IJSRuntime jsRuntime,
    ILogger<HomeDiscoveryGeolocation> logger) : IHomeDiscoveryGeolocation
{
    private const string ModulePath = "/js/home-discovery.js";
    private IJSObjectReference? module;

    public async Task<HomeDiscoveryGeolocationResult> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await (await GetModuleAsync(cancellationToken))
                .InvokeAsync<HomeDiscoveryGeolocationResult>(
                    "getCurrentPosition",
                    cancellationToken);
        }
        catch (Exception exception) when (IsExpectedInteropFailure(exception))
        {
            logger.LogDebug(exception, "Home discovery geolocation is unavailable");
            return new HomeDiscoveryGeolocationResult(HomeDiscoveryGeolocationStatus.Unavailable);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
        {
            return;
        }

        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        module ??= await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            cancellationToken,
            ModulePath);
        return module;
    }

    private static bool IsExpectedInteropFailure(Exception exception) =>
        exception is JSException or
            JSDisconnectedException or
            InvalidOperationException or
            TaskCanceledException;
}
