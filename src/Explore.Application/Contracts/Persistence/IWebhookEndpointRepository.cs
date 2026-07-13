// ABOUTME: Repository contract for webhook endpoints and subscription filtering.
// ABOUTME: Supports LocalProvider endpoint resolution while allowing Svix endpoint mirrors.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public sealed record WebhookEndpointFailureState(
    int ConsecutiveFailureCount,
    bool IsAutoPaused);

public interface IWebhookEndpointRepository
{
    Task<WebhookEndpoint> CreateWithSubscriptionsAsync(
        WebhookEndpoint endpoint,
        IReadOnlyCollection<WebhookEndpointSubscription> subscriptions,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookEndpoint>> ListByTenantAsync(
        Guid tenantId,
        Guid? consumerId,
        int limit,
        CancellationToken cancellationToken);

    Task<WebhookEndpoint?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid endpointId,
        CancellationToken cancellationToken);

    Task<WebhookEndpoint?> GetByTenantAndIdForUpdateAsync(
        Guid tenantId,
        Guid endpointId,
        CancellationToken cancellationToken);

    Task<WebhookEndpoint?> GetByTenantConsumerAndUrlAsync(
        Guid tenantId,
        Guid consumerId,
        string url,
        CancellationToken cancellationToken);

    Task<WebhookEndpoint> UpdateWithSubscriptionsAsync(
        WebhookEndpoint endpoint,
        IReadOnlyCollection<WebhookEndpointSubscription> subscriptions,
        CancellationToken cancellationToken);

    Task<WebhookEndpoint> UpdateAsync(
        WebhookEndpoint endpoint,
        CancellationToken cancellationToken);

    Task ArchiveAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime archivedAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookEndpoint>> GetActiveSubscribedEndpointsAsync(
        Guid tenantId,
        string eventTypeName,
        WebhookProviderMode providerMode,
        CancellationToken cancellationToken);

    Task DisableAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime disabledAt,
        CancellationToken cancellationToken);

    Task MarkSuccessAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime succeededAt,
        CancellationToken cancellationToken);

    Task<WebhookEndpointFailureState> RecordFailureAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime failedAt,
        string failureCategory,
        int autoPauseThreshold,
        CancellationToken cancellationToken);

    Task<bool> TryResumeAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime resumedAt,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
