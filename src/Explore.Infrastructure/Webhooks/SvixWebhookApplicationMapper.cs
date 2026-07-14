// ABOUTME: Verifies Svix application identity against one persisted consumer provider binding.
// ABOUTME: Requires exact UID and tenant/consumer metadata without tenant-only fallback identities.

using Explore.Domain;

namespace Explore.Infrastructure.Webhooks;

internal static class SvixWebhookApplicationMapper
{
    private const string TenantMetadataKey = "islamu.tenant_id";
    private const string ConsumerMetadataKey = "islamu.consumer_id";

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
}
