// ABOUTME: Delegates instance platform monetization settings reads and updates to the generated API client.
// ABOUTME: Preserves generated HAL resources, update DTOs, cancellation, and API failures for bounded UI handling.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;

namespace Explore.Blazor.Client.Services;

public sealed class PlatformMonetizationService(IPlatformMonetizationSettingsClient apiClient) : IPlatformMonetizationService
{
    public Task<HalResourceOfPlatformMonetizationSettingsDto> GetAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetInstancePlatformMonetizationSettingsAsync(cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> UpdateAsync(
        UpdatePlatformMonetizationSettingsDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.UpdateInstancePlatformMonetizationSettingsAsync(request, cancellationToken: cancellationToken);
}
