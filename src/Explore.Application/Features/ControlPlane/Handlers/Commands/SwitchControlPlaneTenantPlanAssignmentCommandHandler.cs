// ABOUTME: Command handler for switching a tenant to a chosen SaaS plan version.
// ABOUTME: Keeps one active assignment by superseding the previous row before creating the new one.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class SwitchControlPlaneTenantPlanAssignmentCommandHandler(
    ITenantPlanRepository tenantPlanRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SwitchControlPlaneTenantPlanAssignmentCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SwitchControlPlaneTenantPlanAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(ExecuteAsync, cancellationToken);

        async Task<BaseCommandResponse<Guid>> ExecuteAsync(CancellationToken token)
        {
            TenantPlanVersion? targetVersion = await tenantPlanRepository.GetVersionAsync(
                request.TenantPlanVersionId,
                token);

            if (targetVersion is null)
            {
                return Failure("Tenant plan version was not found.", ["tenant_plan_version_not_found"]);
            }

            if (targetVersion.TenantPlanStatusId != (int)TenantPlanStatusEnum.Published)
            {
                return Failure("Tenant plan version must be published before assignment.", ["tenant_plan_version_not_published"]);
            }

            TenantPlanAssignment? current = await tenantPlanRepository.GetActiveAssignmentForTenantAsync(
                request.TenantId,
                token);

            if (current?.TenantPlanVersionId == targetVersion.Id)
            {
                return new BaseCommandResponse<Guid>
                {
                    Success = true,
                    Id = current.Id,
                    Message = "Tenant is already assigned to this plan version."
                };
            }

            DateTime now = DateTime.UtcNow;
            if (current is not null)
            {
                current.TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Superseded;
                current.EndedAt = now;
                await tenantPlanRepository.UpdateAssignmentAsync(current, token);
            }

            var assignment = new TenantPlanAssignment
            {
                Id = Guid.CreateVersion7(),
                TenantId = request.TenantId,
                TenantPlan = targetVersion.TenantPlan,
                TenantPlanId = targetVersion.TenantPlanId,
                TenantPlanVersion = targetVersion,
                TenantPlanVersionId = targetVersion.Id,
                TenantPlanAssignmentStatusId = (int)TenantPlanAssignmentStatusEnum.Active,
                AssignedByUserId = request.AssignedByUserId,
                AssignedAt = now
            };

            TenantPlanAssignment created = await tenantPlanRepository.CreateAssignmentAsync(assignment, token);

            return new BaseCommandResponse<Guid>
            {
                Success = true,
                Id = created.Id,
                Message = "Tenant plan assignment switched."
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
