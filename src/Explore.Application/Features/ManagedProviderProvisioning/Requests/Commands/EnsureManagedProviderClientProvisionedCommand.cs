// ABOUTME: MediatR command for trusted provider automation to provision a customer tenant and admin identity.
// ABOUTME: Keeps provider customer authority tenant-scoped; endpoint-level provider/operator authorization is added by API composition.

using Explore.Application.DTOs.ManagedProviderProvisioning;
using Explore.Application.DTOs.Management;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ManagedProviderProvisioning.Requests.Commands;

public sealed record EnsureManagedProviderClientProvisionedCommand : IRequest<BaseCommandResponse<ManagedProviderClientProvisioningResultDto>>
{
    public ManagedProviderClientProvisioningDto ProvisioningDto { get; init; } = null!;
    public ManagementTenantProvisioningRequestDto? ManagementRequest { get; init; }
    public Guid? OperationId { get; init; }
    public Guid? ExpectedOutboxMessageId { get; init; }
}
