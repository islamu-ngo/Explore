// ABOUTME: Maps webhook consumer domain entities into management API DTOs.
// ABOUTME: Keeps Persistence entity-first while centralizing Application-owned projection rules.

using Explore.Application.DTOs.Webhooks;
using Explore.Domain;

namespace Explore.Application.Features.Webhooks.Handlers;

internal static class WebhookConsumerDtoMapper
{
    public static WebhookConsumerDto Map(WebhookConsumer consumer) =>
        new()
        {
            Id = consumer.Id,
            TenantId = consumer.TenantId,
            OwnerActorId = consumer.OwnerActorId,
            OwnerUserId = consumer.OwnerUserId,
            ConsumerKindId = (int)consumer.ConsumerKind,
            ConsumerKindName = consumer.ConsumerKind.ToString(),
            StatusId = (int)consumer.Status,
            StatusName = consumer.Status.ToString(),
            ProviderModeId = (int)consumer.ProviderMode,
            ProviderModeName = consumer.ProviderMode.ToString(),
            Name = consumer.Name,
            ExternalProviderAppId = consumer.ExternalProviderAppId,
            CreatedAt = consumer.CreatedAt,
            UpdatedAt = consumer.UpdatedAt
        };
}
