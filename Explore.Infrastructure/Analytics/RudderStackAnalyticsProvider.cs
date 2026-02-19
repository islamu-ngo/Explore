// ABOUTME: RudderStack analytics provider implementation using RudderStack HTTP API.
// ABOUTME: Avoids process-wide static singleton state to keep tenant configuration isolated.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Analytics;

public class RudderStackAnalyticsProvider : IAnalyticsProvider
{
    private readonly HttpClient _httpClient;
    private readonly IAnalyticsConfigResolver _configResolver;
    private readonly ILogger<RudderStackAnalyticsProvider> _logger;

    public RudderStackAnalyticsProvider(
        HttpClient httpClient,
        IAnalyticsConfigResolver configResolver,
        ILogger<RudderStackAnalyticsProvider> logger)
    {
        _httpClient = httpClient;
        _configResolver = configResolver;
        _logger = logger;
    }

    public Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null, CancellationToken cancellationToken = default)
    {
        return SendAsync("/identify", new
        {
            userId = distinctId,
            traits = traits ?? new Dictionary<string, object>()
        }, cancellationToken);
    }

    public Task TrackAsync(string distinctId, string eventName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return SendAsync("/track", new
        {
            userId = distinctId,
            @event = eventName,
            properties = properties ?? new Dictionary<string, object>()
        }, cancellationToken);
    }

    public Task PageViewAsync(string distinctId, string pagePath, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return SendAsync("/page", new
        {
            userId = distinctId,
            name = pagePath,
            properties = properties ?? new Dictionary<string, object>()
        }, cancellationToken);
    }

    public Task GroupIdentifyAsync(string groupType, string groupKey, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return SendAsync("/group", new
        {
            userId = groupKey,
            groupId = groupKey,
            traits = properties ?? new Dictionary<string, object>(),
            context = new Dictionary<string, object>
            {
                ["groupType"] = groupType
            }
        }, cancellationToken);
    }

    private async Task SendAsync(string path, object payload, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!IsActive(config))
            {
                return;
            }

            var endpoint = (config.EndpointUrl ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}{path}");
            request.Content = JsonContent.Create(payload);

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ApiKey}:"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("RudderStack call {Path} returned status {StatusCode}", path, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RudderStack analytics call failed for {Path}", path);
        }
    }

    private static bool IsActive(AnalyticsConfiguration config)
    {
        return config.Provider == AnalyticsProviderEnum.RudderStack
            && config.IsEnabled
            && !string.IsNullOrWhiteSpace(config.ApiKey);
    }
}
