// ABOUTME: Maps webhook message domain entities into safe management API DTOs.
// ABOUTME: Intentionally omits PayloadJson so delivery history does not leak event payload contents.

using Explore.Application.DTOs.Webhooks;
using Explore.Domain;

namespace Explore.Application.Features.Webhooks.Handlers;

internal static class WebhookMessageDtoMapper
{
    public static WebhookMessageDto Map(WebhookMessage message) =>
        new()
        {
            Id = message.Id,
            TenantId = message.TenantId,
            EventType = message.EventType,
            EventId = message.EventId,
            AggregateKind = message.AggregateKind,
            AggregateId = message.AggregateId,
            ConsumerId = message.ConsumerId,
            ConsumerName = message.Consumer?.Name,
            PayloadHash = message.PayloadHash,
            PayloadRetentionUntil = message.PayloadRetentionUntil,
            PayloadClearedAt = message.PayloadClearedAt,
            ProviderModeId = (int)message.ProviderMode,
            ProviderModeName = message.ProviderMode.ToString(),
            ProviderMessageId = message.ProviderMessageId,
            StatusId = (int)message.Status,
            StatusName = message.Status.ToString(),
            CreatedAt = message.CreatedAt,
            PublishedAt = message.PublishedAt,
            UpdatedAt = message.UpdatedAt
        };
}
