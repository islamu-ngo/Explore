// ABOUTME: Defines CQRS requests for optional Event managed-mode discovery, status, and registration.
// ABOUTME: Keeps the public management surface bounded to instance lifecycle metadata without Event business data.

using Explore.Application.DTOs.Management;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Management.Requests;

public sealed record GetManagementCapabilitiesQuery : IRequest<ManagementCapabilitiesDto>;

public sealed record GetManagedEventInstanceStatusQuery : IRequest<ManagedEventInstanceStatusDto?>;

public sealed record GetManagementHealthQuery : IRequest<ManagementHealthDto>;

public sealed record GetManagementUpgradePreflightQuery(
    string TargetEventVersion,
    string TargetManagementApiVersion) : IRequest<ManagementUpgradePreflightDto>;

public sealed record GetManagementUpgradePostflightQuery(
    string ExpectedEventVersion,
    string ExpectedManagementApiVersion) : IRequest<ManagementUpgradePostflightDto>;

public sealed record TriggerManagedControlPlaneRegistrationCommand
    : IRequest<TriggerManagedRegistrationResult>;

public sealed record RotateManagedControlPlaneCredentialCommand(
    RotateManagedControlPlaneCredentialRequest Request) : IRequest<bool>;

public sealed record RevokeManagedControlPlaneRegistrationCommand : IRequest<bool>;

public sealed record ScheduleManagedTenantProvisioningCommand(
    Guid ManagedInstanceId,
    ManagementTenantProvisioningRequest Request)
    : IRequest<BaseCommandResponse<ManagementTenantProvisioningOperationDto>>;

public sealed record GetManagedTenantProvisioningPreflightQuery(
    Guid ManagedInstanceId,
    ManagementTenantProvisioningRequest Request)
    : IRequest<ManagementTenantProvisioningPreflightDto>;

public sealed record GetManagedTenantProvisioningOperationQuery(
    Guid ManagedInstanceId,
    Guid OperationId) : IRequest<ManagementTenantProvisioningOperationDto?>;

public sealed record CancelManagedTenantProvisioningOperationCommand(
    Guid ManagedInstanceId,
    Guid OperationId) : IRequest<BaseCommandResponse<ManagementTenantProvisioningOperationDto>>;

public sealed record ProcessManagedTenantProvisioningOperationCommand(
    Guid OperationId,
    Guid OutboxMessageId) : IRequest<bool>;

public sealed record ReconcileManagedTenantProvisioningDeadLetterCommand(
    Guid OperationId,
    Guid OutboxMessageId) : IRequest<bool>;
