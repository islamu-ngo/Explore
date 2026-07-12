// ABOUTME: Reusable Application boundary for the existing managed-provider tenant transaction.
// ABOUTME: Lets durable Event management operations reuse provisioning without coupling handlers to handlers.

using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.DTOs.Management;
using Explore.Application.Responses;

namespace Explore.Application.Features.ManagedProviderProvisioning;

public interface IManagedProviderClientProvisioner
{
    Task<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>> EnsureAsync(
        ManagedProviderClientProvisioningDto provisioningDto,
        ManagementTenantProvisioningRequest? managementRequest,
        Guid? operationId,
        Guid? expectedOutboxMessageId,
        CancellationToken cancellationToken);
}
