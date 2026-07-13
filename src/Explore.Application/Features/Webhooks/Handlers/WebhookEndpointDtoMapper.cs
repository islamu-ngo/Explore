// ABOUTME: Maps webhook endpoint domain entities into management API DTOs.
// ABOUTME: Keeps Persistence entity-first and prevents secret refs from crossing into read contracts.

using Explore.Application.DTOs.Webhooks;
using Explore.Domain;

namespace Explore.Application.Features.Webhooks.Handlers;

internal static class WebhookEndpointDtoMapper
{
    public static WebhookEndpointDto Map(WebhookEndpoint endpoint) =>
        new()
        {
            Id = endpoint.Id,
            TenantId = endpoint.TenantId,
            ConsumerId = endpoint.ConsumerId,
            ConsumerName = endpoint.Consumer?.Name,
            ProviderModeId = (int)(endpoint.Consumer?.ProviderMode ?? WebhookProviderMode.Local),
            ProviderModeName = (endpoint.Consumer?.ProviderMode ?? WebhookProviderMode.Local).ToString(),
            Url = endpoint.Url,
            Description = endpoint.Description,
            StatusId = (int)endpoint.Status,
            StatusName = endpoint.Status.ToString(),
            SecretVersion = endpoint.SecretVersion,
            ProviderEndpointId = endpoint.ProviderEndpointId,
            MaxAttempts = endpoint.MaxAttempts,
            TimeoutSeconds = endpoint.TimeoutSeconds,
            RateLimitPerMinute = endpoint.RateLimitPerMinute,
            LastSuccessAt = endpoint.LastSuccessAt,
            LastFailureAt = endpoint.LastFailureAt,
            ConsecutiveFailureCount = endpoint.ConsecutiveFailureCount,
            CircuitOpenedAt = endpoint.CircuitOpenedAt,
            AutoPausedAt = endpoint.AutoPausedAt,
            AutoPauseReason = endpoint.AutoPauseReason,
            LastResumedAt = endpoint.LastResumedAt,
            DeliveryStateVersion = endpoint.DeliveryStateVersion,
            CreatedAt = endpoint.CreatedAt,
            UpdatedAt = endpoint.UpdatedAt,
            Subscriptions = endpoint.Subscriptions
                .OrderBy(subscription => subscription.EventType?.GroupName)
                .ThenBy(subscription => subscription.EventType?.Name)
                .Select(MapSubscription)
                .ToArray()
        };

    private static WebhookEndpointSubscriptionDto MapSubscription(WebhookEndpointSubscription subscription) =>
        new()
        {
            Id = subscription.Id,
            EventTypeId = subscription.EventTypeId,
            EventTypeName = subscription.EventType?.Name ?? string.Empty,
            EventTypeGroupName = subscription.EventType?.GroupName ?? string.Empty,
            IsEnabled = subscription.IsEnabled,
            CreatedAt = subscription.CreatedAt
        };
}
