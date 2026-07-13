// ABOUTME: Shared Svix application mapping helper for webhook delivery and portal access.
// ABOUTME: Keeps tenant/consumer-to-Svix UID logic deterministic across all Svix provider services.

using Explore.Domain;

namespace Explore.Infrastructure.Webhooks;

internal static class SvixWebhookApplicationMapper
{
    private const string TenantMetadataKey = "islamu.tenant_id";
    private const string ConsumerMetadataKey = "islamu.consumer_id";

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

    public static bool IsVerifiedConsumerBinding(
        SvixApplicationBindingResult application,
        Guid tenantId,
        WebhookConsumer consumer,
        WebhookConsumerProviderBinding binding)
    {
        return string.Equals(application.AppId, binding.ExternalApplicationId, StringComparison.Ordinal)
            && string.Equals(application.AppUid, binding.ApplicationUid, StringComparison.Ordinal)
            && application.Metadata.TryGetValue(TenantMetadataKey, out var boundTenantId)
            && string.Equals(boundTenantId, tenantId.ToString("D"), StringComparison.OrdinalIgnoreCase)
            && application.Metadata.TryGetValue(ConsumerMetadataKey, out var boundConsumerId)
            && string.Equals(boundConsumerId, consumer.Id.ToString("D"), StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateConsumerAppUid(Guid consumerId) => $"islamu-consumer-{consumerId:N}";

    private static string ResolveAppUid(Guid tenantId, Guid? consumerId, WebhookConsumer? consumer)
    {
        var verifiedBinding = consumer?.GetVerifiedProviderBinding(WebhookProviderKind.Svix);
        if (verifiedBinding is not null)
        {
            return verifiedBinding.ApplicationUid;
        }

        if (consumerId is { } id)
        {
            return CreateConsumerAppUid(id);
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
            [TenantMetadataKey] = tenantId.ToString("D")
        };

        if (consumerId is { } id)
        {
            metadata[ConsumerMetadataKey] = id.ToString("D");
        }

        if (consumer is not null)
        {
            metadata["islamu.consumer_kind"] = consumer.ConsumerKind.ToString();
        }

        return metadata;
    }
}
