// ABOUTME: Applies audited tenant lifecycle status transitions for the control plane.
// ABOUTME: Records destructive purge intent without deleting tenant data in the request path.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class TransitionControlPlaneTenantLifecycleCommandHandler(
    ITenantRepository tenantRepository,
    ITenantLifecycleLogRepository lifecycleLogRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<TransitionControlPlaneTenantLifecycleCommand, BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>
{
    public async Task<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>> Handle(
        TransitionControlPlaneTenantLifecycleCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
        {
            return Failure("Authenticated operator context is required.");
        }

        var reason = NormalizeReason(request.Reason);
        if (RequiresReason(request.TargetStatus) && string.IsNullOrWhiteSpace(reason))
        {
            return Failure($"{request.TargetStatus} requires a reason.");
        }

        return await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tenant = await tenantRepository.GetById(request.TenantId);
            if (tenant is null)
            {
                return Failure("Tenant was not found.");
            }

            if (RequiresConfirmation(request.TargetStatus) && !IsConfirmed(request.ConfirmationText, tenant.Slug))
            {
                return Failure($"{request.TargetStatus} requires confirmation with tenant slug '{tenant.Slug}'.");
            }

            var currentStatus = (TenantStatusEnum)tenant.TenantStatusId;
            if (!IsAllowedTransition(currentStatus, request.TargetStatus))
            {
                return Failure($"Cannot transition tenant from {currentStatus} to {request.TargetStatus}.");
            }

            var transitionedAt = DateTime.UtcNow;
            if (currentStatus == request.TargetStatus)
            {
                return Success(ControlPlaneTenantMapper.ToTransition(
                    tenant.Id,
                    tenant.TenantStatusId,
                    tenant.TenantStatusId,
                    userId.Value,
                    reason,
                    transitionedAt),
                    "Tenant already has the requested lifecycle status.");
            }

            var oldStatusId = tenant.TenantStatusId;
            tenant.TenantStatusId = (int)request.TargetStatus;
            tenant.TenantStatus = null!;
            tenant.UpdatedAt = transitionedAt;
            tenant.UpdatedBy = userId.Value;

            await tenantRepository.Update(tenant);
            await lifecycleLogRepository.Create(new TenantLifecycleLog
            {
                TenantId = tenant.Id,
                Tenant = null!,
                OldStatusId = oldStatusId,
                NewStatusId = tenant.TenantStatusId,
                NewStatus = null!,
                TransitionedByUserId = userId.Value,
                Reason = reason,
                TransitionedAt = transitionedAt,
                CreatedAt = transitionedAt,
                CreatedBy = userId.Value
            });

            return Success(ControlPlaneTenantMapper.ToTransition(
                tenant.Id,
                oldStatusId,
                tenant.TenantStatusId,
                userId.Value,
                reason,
                transitionedAt),
                request.TargetStatus is TenantStatusEnum.Purged
                    ? "Tenant purge scheduled."
                    : "Tenant lifecycle status updated.");
        }, cancellationToken);
    }

    private static bool RequiresReason(TenantStatusEnum targetStatus) =>
        targetStatus is TenantStatusEnum.Suspended or TenantStatusEnum.Archived or TenantStatusEnum.Purged;

    private static bool RequiresConfirmation(TenantStatusEnum targetStatus) =>
        targetStatus is TenantStatusEnum.Purged;

    private static bool IsConfirmed(string? confirmationText, string tenantSlug) =>
        string.Equals(confirmationText?.Trim(), tenantSlug, StringComparison.Ordinal);

    private static bool IsAllowedTransition(TenantStatusEnum currentStatus, TenantStatusEnum targetStatus) =>
        currentStatus switch
        {
            TenantStatusEnum.Purged => targetStatus == TenantStatusEnum.Purged,
            TenantStatusEnum.Active => targetStatus is TenantStatusEnum.Active or TenantStatusEnum.Suspended or TenantStatusEnum.Archived,
            TenantStatusEnum.Provisioning => targetStatus is TenantStatusEnum.Provisioning or TenantStatusEnum.Active or TenantStatusEnum.Suspended or TenantStatusEnum.Archived,
            TenantStatusEnum.Suspended => targetStatus is TenantStatusEnum.Suspended or TenantStatusEnum.Active or TenantStatusEnum.Archived,
            TenantStatusEnum.Archived => targetStatus is TenantStatusEnum.Archived or TenantStatusEnum.Active or TenantStatusEnum.Purged,
            _ => false
        };

    private static string? NormalizeReason(string? reason)
    {
        var trimmed = reason?.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
            ? null
            : trimmed.Length > 1000 ? trimmed[..1000] : trimmed;
    }

    private static BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto> Success(
        ControlPlaneTenantLifecycleTransitionDto result,
        string message) => new()
        {
            Success = true,
            Id = result,
            Message = message
        };

    private static BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto> Failure(string message) => new()
    {
        Success = false,
        Message = message,
        Errors = [message]
    };
}
