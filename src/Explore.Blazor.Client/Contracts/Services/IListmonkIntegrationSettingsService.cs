// ABOUTME: Client contract for Listmonk integration settings and connection testing.
// ABOUTME: Keeps Listmonk admin UI behind generated-client service calls.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Webhooks;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IListmonkIntegrationSettingsService
{
    Task<ListmonkIntegrationSettingsDto?> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<WebhookActionResult> UpdateSettingsAsync(
        UpdateListmonkIntegrationSettingsDto request,
        CancellationToken cancellationToken = default);

    Task<WebhookActionResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}
