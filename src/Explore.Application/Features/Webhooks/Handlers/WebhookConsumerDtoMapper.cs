// ABOUTME: Maps webhook consumer domain entities into management API DTOs.
// ABOUTME: Keeps Persistence entity-first while centralizing Application-owned projection rules.

using Explore.Application.DTOs.Webhooks;
using Explore.Application.Lookups;
using Explore.Domain;

namespace Explore.Application.Features.Webhooks.Handlers;

internal static class WebhookConsumerDtoMapper
{
    public static WebhookConsumerDto Map(WebhookConsumer consumer)
    {
        var consumerKind = NormalizedLookupMetadata.WebhookConsumerKind(consumer.ConsumerKindId);
        var status = NormalizedLookupMetadata.WebhookConsumerStatus(consumer.StatusId);
        var providerMode = NormalizedLookupMetadata.WebhookProviderMode(consumer.ProviderModeId);
        return new WebhookConsumerDto
        {
            Id = consumer.Id,
            TenantId = consumer.TenantId,
            OwnerActorId = consumer.OwnerActorId,
            OwnerUserId = consumer.OwnerUserId,
            ConsumerKindId = consumerKind.Id,
            ConsumerKindCode = consumerKind.Code,
            ConsumerKindName = consumerKind.Name,
            StatusId = status.Id,
            StatusCode = status.Code,
            StatusName = status.Name,
            ProviderModeId = providerMode.Id,
            ProviderModeCode = providerMode.Code,
            ProviderModeName = providerMode.Name,
            Name = consumer.Name,
            CreatedAt = consumer.CreatedAt,
            UpdatedAt = consumer.UpdatedAt
        };
    }
}
