// ABOUTME: Typed browser boundary for an explicit one-shot home-discovery geolocation request.
// ABOUTME: Returns transient coordinates only to the caller so they can be reduced to a coarse configured area.

namespace Explore.Blazor.Client.Contracts.Interop;

public interface IHomeDiscoveryGeolocation : IAsyncDisposable
{
    Task<HomeDiscoveryGeolocationResult> GetCurrentPositionAsync(
        CancellationToken cancellationToken = default);
}
