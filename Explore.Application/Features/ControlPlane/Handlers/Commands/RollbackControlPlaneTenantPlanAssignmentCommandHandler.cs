// ABOUTME: Command handler for reactivating a previous tenant plan assignment.
// ABOUTME: Marks the current active assignment as rolled back without applying settings side effects.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class RollbackControlPlaneTenantPlanAssignmentCommandHandler(ITenantPlanRepository tenantPlanRepository)
    : IRequestHandler<RollbackControlPlaneTenantPlanAssignmentCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        RollbackControlPlaneTenantPlanAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        TenantPlanAssignment? previous = await tenantPlanRepository.GetAssignmentAsync(request.AssignmentId, cancellationToken);
        if (previous is null)
        {
            return Failure("Tenant plan assignment was not found.", ["tenant_plan_assignment_not_found"]);
        }

        if (previous.TenantId != request.TenantId)
        {
            return Failure("Tenant plan assignment does not belong to the requested tenant.", ["tenant_plan_assignment_tenant_mismatch"]);
        }

        TenantPlanAssignment? current = await tenantPlanRepository.GetActiveAssignmentForTenantAsync(
            request.TenantId,
            cancellationToken);

        DateTime now = DateTime.UtcNow;
        if (current is not null && current.Id != previous.Id)
        {
            current.TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.RolledBack;
            current.EndedAt = now;
            await tenantPlanRepository.UpdateAssignmentAsync(current, cancellationToken);
        }

        previous.TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active;
        previous.EndedAt = null;
        await tenantPlanRepository.UpdateAssignmentAsync(previous, cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = previous.Id,
            Message = "Tenant plan assignment rolled back."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
