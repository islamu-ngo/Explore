// ABOUTME: Blazor JS interop wrapper for provider-agnostic analytics bridge functions.
// ABOUTME: Initializes provider adapter from public settings payload and safely no-ops on JS failures.

using Microsoft.JSInterop;

namespace Explore.Blazor.Client.Services;

public interface IAnalyticsInterop
{
    Task InitAsync(string analyticsProvider, bool analyticsEnabled, string? apiKey, string? endpointUrl);
    Task TrackAsync(string eventName, IDictionary<string, object>? properties = null);
    Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null);
    Task PageViewAsync(string pagePath, IDictionary<string, object>? properties = null);
}

public class AnalyticsInterop : IAnalyticsInterop, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AnalyticsInterop> _logger;
    private IJSObjectReference? _module;

    public AnalyticsInterop(IJSRuntime jsRuntime, ILogger<AnalyticsInterop> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task InitAsync(string analyticsProvider, bool analyticsEnabled, string? apiKey, string? endpointUrl)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("initAnalytics", analyticsProvider, analyticsEnabled, apiKey ?? string.Empty, endpointUrl ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics bridge initialization failed");
        }
    }

    public async Task TrackAsync(string eventName, IDictionary<string, object>? properties = null)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("trackEvent", eventName, properties ?? new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics bridge track failed for event {EventName}", eventName);
        }
    }

    public async Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("identifyUser", distinctId, traits ?? new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics bridge identify failed for {DistinctId}", distinctId);
        }
    }

    public async Task PageViewAsync(string pagePath, IDictionary<string, object>? properties = null)
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("trackPageView", pagePath, properties ?? new Dictionary<string, object>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Analytics bridge page view failed for {PagePath}", pagePath);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // no-op
            }
        }
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/analytics-bridge.js");
        return _module;
    }
}
