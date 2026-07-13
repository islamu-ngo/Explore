// ABOUTME: Command handler for reactivating a previous tenant plan assignment.
// ABOUTME: Marks the current active assignment as rolled back without applying settings side effects.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class RollbackControlPlaneTenantPlanAssignmentCommandHandler(
    ITenantPlanRepository tenantPlanRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RollbackControlPlaneTenantPlanAssignmentCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        RollbackControlPlaneTenantPlanAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(ExecuteAsync, cancellationToken);

        async Task<BaseCommandResponse<Guid>> ExecuteAsync(CancellationToken token)
        {
            TenantPlanAssignment? current = await tenantPlanRepository.GetActiveAssignmentForTenantAsync(
                request.TenantId,
                token);
            if (current is null)
            {
                return Failure("Tenant has no active plan assignment.", ["tenant_plan_active_assignment_not_found"]);
            }

            TenantPlanAssignment? previous = await tenantPlanRepository.GetPreviousEligibleAssignmentForTenantAsync(
                request.TenantId,
                current.Id,
                token);
            if (previous is null)
            {
                return Failure("Tenant has no eligible previous plan assignment.", ["tenant_plan_rollback_assignment_not_found"]);
            }

            if (previous.Id != request.AssignmentId
                || previous.TenantId != request.TenantId
                || previous.TenantPlanAssignmentStatusId != (int)TenantPlanAssignmentStatusEnum.Superseded
                || previous.TenantPlanVersion.TenantPlanStatusId != (int)TenantPlanStatusEnum.Published
                || previous.EndedAt is null
                || previous.EndedAt > current.AssignedAt)
            {
                return Failure("Tenant plan assignment is not the eligible rollback target.", ["tenant_plan_rollback_assignment_not_eligible"]);
            }

            DateTime now = DateTime.UtcNow;
            current.TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.RolledBack;
            current.EndedAt = now;
            await tenantPlanRepository.UpdateAssignmentAsync(current, token);

            previous.TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active;
            previous.EndedAt = null;
            await tenantPlanRepository.UpdateAssignmentAsync(previous, token);

            return new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = previous.Id,
                Message = "Tenant plan assignment rolled back."
            };
        }
    }

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
