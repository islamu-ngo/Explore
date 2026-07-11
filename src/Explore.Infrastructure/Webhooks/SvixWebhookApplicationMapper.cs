// ABOUTME: Shared Svix application mapping helper for webhook delivery and portal access.
// ABOUTME: Keeps tenant/consumer-to-Svix UID logic deterministic across all Svix provider services.

using Explore.Domain;

namespace Explore.Infrastructure.Webhooks;

internal static class SvixWebhookApplicationMapper
{
    public static SvixApplicationSyncRequest CreateSyncRequest(
        Guid tenantId,
        Guid? consumerId,
        WebhookConsumer? consumer)
    {
        var appUid = ResolveAppUid(tenantId, consumerId, consumer);
        return new SvixApplicationSyncRequest(
            tenantId,
            appUid,
            ResolveAppName(tenantId, consumerId, consumer),
            CreateMetadata(tenantId, consumerId, consumer),
            $"svix-app:{appUid}");
    }

    private static string ResolveAppUid(Guid tenantId, Guid? consumerId, WebhookConsumer? consumer)
    {
        if (!string.IsNullOrWhiteSpace(consumer?.ExternalProviderAppId))
        {
            return consumer.ExternalProviderAppId.Trim();
        }

        if (consumerId is { } id)
        {
            return $"islamu-consumer-{id:N}";
        }

        return $"islamu-tenant-{tenantId:N}";
    }

    private static string ResolveAppName(Guid tenantId, Guid? consumerId, WebhookConsumer? consumer)
    {
        if (!string.IsNullOrWhiteSpace(consumer?.Name))
        {
            return consumer.Name;
        }

        if (consumerId is { } id)
        {
            return $"ISLAMU webhook consumer {id:D}";
        }

        return $"ISLAMU tenant {tenantId:D}";
    }

    private static IReadOnlyDictionary<string, string> CreateMetadata(
        Guid tenantId,
        Guid? consumerId,
        WebhookConsumer? consumer)
    {
        var metadata = new Dictionary<string, string>
        {
            ["islamu.tenant_id"] = tenantId.ToString("D")
        };

        if (consumerId is { } id)
        {
            metadata["islamu.consumer_id"] = id.ToString("D");
        }

        if (consumer is not null)
        {
            metadata["islamu.consumer_kind"] = consumer.ConsumerKind.ToString();
        }

        return metadata;
    }
}
