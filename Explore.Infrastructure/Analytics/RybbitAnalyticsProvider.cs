// ABOUTME: Rybbit analytics provider implementation using Rybbit HTTP tracking API.
// ABOUTME: Supports event/page tracking with tenant-safe fire-and-forget behavior.

using System.Net.Http.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Analytics;

public class RybbitAnalyticsProvider : IAnalyticsProvider
{
    private readonly HttpClient _httpClient;
    private readonly IAnalyticsConfigResolver _configResolver;
    private readonly ILogger<RybbitAnalyticsProvider> _logger;

    public RybbitAnalyticsProvider(
        HttpClient httpClient,
        IAnalyticsConfigResolver configResolver,
        ILogger<RybbitAnalyticsProvider> logger)
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
        return SendAsync("event", distinctId, eventName, properties, cancellationToken);
    }

    public Task PageViewAsync(string distinctId, string pagePath, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var payload = properties is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(properties);
        payload["pagePath"] = pagePath;
        return SendAsync("pageview", distinctId, pagePath, payload, cancellationToken);
    }

    public Task GroupIdentifyAsync(string groupType, string groupKey, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private async Task SendAsync(string type, string distinctId, string name, IDictionary<string, object>? properties, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!IsActive(config) || string.IsNullOrWhiteSpace(config.EndpointUrl))
            {
                return;
            }

            var payload = new
            {
                type,
                name,
                userId = distinctId,
                siteId = config.ApiKey,
                properties = properties ?? new Dictionary<string, object>()
            };

            using var response = await _httpClient.PostAsJsonAsync($"{config.EndpointUrl.TrimEnd('/')}/api/track", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Rybbit call returned status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rybbit analytics call failed for {Type}:{Name}", type, name);
        }
    }

    private static bool IsActive(AnalyticsConfiguration config)
    {
        return config.Provider == AnalyticsProviderEnum.Rybbit
            && config.IsEnabled
            && !string.IsNullOrWhiteSpace(config.ApiKey);
    }
}
