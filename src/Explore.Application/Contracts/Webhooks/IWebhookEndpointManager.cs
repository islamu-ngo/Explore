// ABOUTME: Provider-neutral endpoint management boundary for webhook endpoints.
// ABOUTME: Keeps endpoint creation, update, and disable behavior behind application contracts.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookEndpointManager
{
    Task<WebhookEndpointResult> CreateEndpointAsync(
        CreateWebhookEndpointInput input,
        CancellationToken cancellationToken);

    Task<WebhookEndpointResult> UpdateEndpointAsync(
        UpdateWebhookEndpointInput input,
        CancellationToken cancellationToken);

    Task DisableEndpointAsync(Guid endpointId, CancellationToken cancellationToken);
}
