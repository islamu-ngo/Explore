// ABOUTME: Plausible analytics provider implementation using Plausible Events API.
// ABOUTME: Implements event/page tracking with safe no-op behavior when config is incomplete.

using System.Net.Http.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Analytics;

public class PlausibleAnalyticsProvider : IAnalyticsProvider
{
    private readonly HttpClient _httpClient;
    private readonly IAnalyticsConfigResolver _configResolver;
    private readonly ILogger<PlausibleAnalyticsProvider> _logger;

    private const string DefaultPlausibleHost = "https://plausible.io";

    public PlausibleAnalyticsProvider(
        HttpClient httpClient,
        IAnalyticsConfigResolver configResolver,
        ILogger<PlausibleAnalyticsProvider> logger)
    {
        _httpClient = httpClient;
        _configResolver = configResolver;
        _logger = logger;
    }

    public Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task TrackAsync(string distinctId, string eventName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return SendEventAsync(eventName, properties, cancellationToken);
    }

    public Task PageViewAsync(string distinctId, string pagePath, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var payload = properties is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(properties);
        payload["url"] = pagePath;
        return SendEventAsync("pageview", payload, cancellationToken);
    }

    public Task GroupIdentifyAsync(string groupType, string groupKey, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private async Task SendEventAsync(string eventName, IDictionary<string, object>? properties, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!IsActive(config))
            {
                return;
            }

            var host = string.IsNullOrWhiteSpace(config.EndpointUrl)
                ? DefaultPlausibleHost
                : config.EndpointUrl!;

            var requestPayload = new
            {
                name = eventName,
                domain = config.ApiKey,
                url = properties is not null && properties.TryGetValue("url", out var value) ? value?.ToString() ?? "/" : "/",
                props = properties ?? new Dictionary<string, object>()
            };

            using var response = await _httpClient.PostAsJsonAsync($"{host.TrimEnd('/')}/api/event", requestPayload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Plausible call returned status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Plausible analytics call failed for event {EventName}", eventName);
        }
    }

    private static bool IsActive(AnalyticsConfiguration config)
    {
        return config.Provider == Explore.Domain.Enums.AnalyticsProviderEnum.Plausible
            && config.IsEnabled
            && !string.IsNullOrWhiteSpace(config.ApiKey);
    }
}
