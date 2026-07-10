// ABOUTME: Browser-owned Web Push permission and subscription state returned through JavaScript interop.
// ABOUTME: Carries only public enrollment material required by the authenticated BFF subscription flow.

namespace Explore.Blazor.Client.Contracts.Interop;

public sealed record WebPushBrowserState(
    bool IsSupported,
    string Permission,
    bool HasSubscription,
    string DeviceIdentifier);

public sealed record WebPushBrowserSubscription(
    string DeviceIdentifier,
    string Endpoint,
    string P256Dh,
    string Auth,
    DateTimeOffset? ExpirationTime);
