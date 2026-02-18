using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Services;

public sealed class ServerAnalyticsInterop : IAnalyticsInterop
{
    public Task InitAsync(string analyticsProvider, bool analyticsEnabled, string? apiKey, string? endpointUrl)
    {
        return Task.CompletedTask;
    }

    public Task TrackAsync(string eventName, IDictionary<string, object>? properties = null)
    {
        return Task.CompletedTask;
    }

    public Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null)
    {
        return Task.CompletedTask;
    }

    public Task PageViewAsync(string pagePath, IDictionary<string, object>? properties = null)
    {
        return Task.CompletedTask;
    }
}
