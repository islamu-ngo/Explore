// ABOUTME: Contract for analytics JS interop bridge abstraction.
// ABOUTME: Enables testable analytics tracking independent of concrete interop implementation.

namespace Explore.Blazor.Client.Contracts.Interop;

public interface IAnalyticsInterop
{
    Task InitAsync(string analyticsProvider, bool analyticsEnabled, string analyticsConsentMode, string analyticsTransportMode, bool allowIdentify, string? apiKey, string? endpointUrl);
    Task TrackAsync(string eventName, IDictionary<string, object>? properties = null);
    Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null);
    Task PageViewAsync(string pagePath, IDictionary<string, object>? properties = null);
}
