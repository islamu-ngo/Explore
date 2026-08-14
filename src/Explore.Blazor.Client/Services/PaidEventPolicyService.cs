// ABOUTME: Delegates paid-event policy reads and writes to the generated API client.
// ABOUTME: Preserves generated HAL resources, configuration payloads, and cancellation tokens.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.PaidEventPolicies;

namespace Explore.Blazor.Client.Services;

public sealed class PaidEventPolicyService(IEventApiClient apiClient) : IPaidEventPolicyService
{
    public Task<HalResourceOfPaidEventPolicyDto> GetInstanceAsync(CancellationToken cancellationToken = default) =>
        apiClient.GetInstancePaidEventPolicySettingsAsync(cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> UpdateInstanceAsync(
        RevisePaidEventPolicyDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.UpdateInstancePaidEventPolicySettingsAsync(request, cancellationToken: cancellationToken);

    public Task<HalResourceOfTenantPaidEventPolicyConfigurationDto> GetTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetTenantPaidEventPolicySettingsAsync(tenantId, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> UpdateTenantAsync(
        Guid tenantId,
        RevisePaidEventPolicyDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.UpdateTenantPaidEventPolicySettingsAsync(tenantId, request, cancellationToken: cancellationToken);
}
