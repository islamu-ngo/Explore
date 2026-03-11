using Explore.Blazor.Client.Contracts.Interop;
using Explore.Blazor.Client.Models.Analytics;

namespace Explore.Blazor.Services;

public sealed class ServerAnalyticsInterop : IAnalyticsInterop
{
    public Task InitAsync(string analyticsProvider, bool analyticsEnabled, string analyticsConsentMode, string analyticsTransportMode, bool allowIdentify, string? apiKey, string? endpointUrl, PosthogClientBootstrapModel? posthogOptions = null)
        => Task.CompletedTask;

    public Task TrackAsync(string eventName, IDictionary<string, object>? properties = null)
        => Task.CompletedTask;

    public Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null)
        => Task.CompletedTask;

    public Task PageViewAsync(string pagePath, IDictionary<string, object>? properties = null)
        => Task.CompletedTask;

    public Task OptInCapturingAsync() => Task.CompletedTask;

    public Task OptOutCapturingAsync() => Task.CompletedTask;
}
