// ABOUTME: Persistence contract for verified webhook consumer-to-provider bindings.
// ABOUTME: Returns domain entities and exposes fenced verification-state transitions.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IWebhookConsumerProviderBindingRepository
{
    Task<WebhookConsumerProviderBinding> CreateAsync(
        WebhookConsumerProviderBinding binding,
        CancellationToken cancellationToken);

    Task<WebhookConsumerProviderBinding?> GetVerifiedByConsumerAsync(
        Guid tenantId,
        Guid webhookConsumerId,
        WebhookProviderKind providerKind,
        string providerEnvironment,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookConsumerProviderBinding>> GetVerifiedByConsumersAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> webhookConsumerIds,
        WebhookProviderKind providerKind,
        string providerEnvironment,
        CancellationToken cancellationToken);

    Task<WebhookConsumerProviderBinding?> GetVerifiedByProviderIdentityAsync(
        Guid tenantId,
        WebhookProviderKind providerKind,
        string providerEnvironment,
        string externalApplicationId,
        string applicationUid,
        CancellationToken cancellationToken);

    Task<bool> TryVerifyAsync(
        Guid tenantId,
        Guid bindingId,
        long expectedConcurrencyVersion,
        long expectedVerificationFence,
        string externalApplicationId,
        DateTimeOffset verifiedAtUtc,
        CancellationToken cancellationToken);

    Task<bool> TryDisableAsync(
        Guid tenantId,
        Guid bindingId,
        long expectedConcurrencyVersion,
        long expectedVerificationFence,
        DateTimeOffset disabledAtUtc,
        CancellationToken cancellationToken);

    Task<bool> TryRebindAsync(
        Guid tenantId,
        Guid bindingId,
        long expectedConcurrencyVersion,
        long expectedVerificationFence,
        string externalApplicationId,
        DateTimeOffset verifiedAtUtc,
        CancellationToken cancellationToken);
}
