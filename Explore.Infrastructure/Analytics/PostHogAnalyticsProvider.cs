// ABOUTME: PostHog analytics provider implementation using HTTP API endpoints.
// ABOUTME: Implements thin abstraction methods and PostHog feature-flag capability with safe defaults.

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Models;
using Explore.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Analytics;

/// <summary>
/// PostHog analytics provider using the PostHog HTTP Capture and Decide APIs.
/// Implements both event tracking and feature flag evaluation.
/// All calls are fire-and-forget safe — errors are caught, logged, and swallowed.
/// </summary>
public class PostHogAnalyticsProvider : IAnalyticsProvider, IAnalyticsFeatureFlagProvider
{
    private readonly HttpClient _httpClient;
    private readonly IAnalyticsConfigResolver _configResolver;
    private readonly ILogger<PostHogAnalyticsProvider> _logger;

    private const string DefaultPostHogHost = "https://us.i.posthog.com";

    public PostHogAnalyticsProvider(
        HttpClient httpClient,
        IAnalyticsConfigResolver configResolver,
        ILogger<PostHogAnalyticsProvider> logger)
    {
        _httpClient = httpClient;
        _configResolver = configResolver;
        _logger = logger;
    }

    public Task IdentifyAsync(string distinctId, IDictionary<string, object>? traits = null, CancellationToken cancellationToken = default)
    {
        var properties = new Dictionary<string, object>
        {
            ["$set"] = traits ?? new Dictionary<string, object>()
        };

        return CaptureAsync(distinctId, "$identify", properties, cancellationToken);
    }

    public Task TrackAsync(string distinctId, string eventName, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        return CaptureAsync(distinctId, eventName, properties, cancellationToken);
    }

    public Task PageViewAsync(string distinctId, string pagePath, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var payload = properties is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(properties);
        payload["$current_url"] = pagePath;

        return CaptureAsync(distinctId, "$pageview", payload, cancellationToken);
    }

    public Task GroupIdentifyAsync(string groupType, string groupKey, IDictionary<string, object>? properties = null, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["$group_type"] = groupType,
            ["$group_key"] = groupKey
        };

        if (properties is not null)
        {
            payload["$group_set"] = properties;
        }

        return CaptureAsync(groupKey, "$groupidentify", payload, cancellationToken);
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureKey, string distinctId, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!IsActive(config) || string.IsNullOrWhiteSpace(config.PersonalApiKey))
            {
                return false;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(config, "/decide/?v=3"));
            request.Content = JsonContent.Create(new
            {
                api_key = config.ApiKey,
                distinct_id = distinctId,
                groups = new { }
            });
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.PersonalApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("featureFlags", out var featureFlags) || featureFlags.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!featureFlags.TryGetProperty(featureKey, out var featureValue))
            {
                return false;
            }

            return featureValue.ValueKind == JsonValueKind.True;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostHog feature flag check failed for {FeatureKey}", featureKey);
            return false;
        }
    }

    public async Task<object?> GetFeatureFlagPayloadAsync(string featureKey, string distinctId, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!IsActive(config) || string.IsNullOrWhiteSpace(config.PersonalApiKey))
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(config, "/decide/?v=3"));
            request.Content = JsonContent.Create(new
            {
                api_key = config.ApiKey,
                distinct_id = distinctId,
                groups = new { }
            });
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.PersonalApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("featureFlagPayloads", out var payloads) || payloads.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!payloads.TryGetProperty(featureKey, out var payloadValue))
            {
                return null;
            }

            return payloadValue.GetRawText();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostHog feature flag payload fetch failed for {FeatureKey}", featureKey);
            return null;
        }
    }

    private async Task CaptureAsync(string distinctId, string eventName, IDictionary<string, object>? properties, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _configResolver.ResolveAsync(cancellationToken);
            if (!IsActive(config))
            {
                return;
            }

            var payload = new
            {
                api_key = config.ApiKey,
                @event = eventName,
                distinct_id = distinctId,
                properties = properties ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(config, "/capture/"))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("PostHog capture returned status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostHog analytics call failed for event {EventName}", eventName);
        }
    }

    private static bool IsActive(AnalyticsConfiguration config)
    {
        return config.Provider == AnalyticsProviderEnum.Posthog
            && config.IsEnabled
            && !string.IsNullOrWhiteSpace(config.ApiKey);
    }

    private static string BuildUrl(AnalyticsConfiguration config, string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(config.EndpointUrl) ? DefaultPostHogHost : config.EndpointUrl;
        return $"{baseUrl.TrimEnd('/')}{path}";
    }
}
