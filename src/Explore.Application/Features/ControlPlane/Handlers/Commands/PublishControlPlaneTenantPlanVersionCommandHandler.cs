// ABOUTME: Command handler for publishing tenant plan versions with explicit assignment policy.
// ABOUTME: Supports pinning existing tenants or moving active assignments to the published version.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class PublishControlPlaneTenantPlanVersionCommandHandler(ITenantPlanRepository tenantPlanRepository)
    : IRequestHandler<PublishControlPlaneTenantPlanVersionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        PublishControlPlaneTenantPlanVersionCommand request,
        CancellationToken cancellationToken)
    {
        TenantPlanVersion? version = await tenantPlanRepository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version is null)
        {
            return Failure("Tenant plan version was not found.", ["tenant_plan_version_not_found"]);
        }

        version.TenantPlanStatusId = (int)TenantPlanStatusEnum.Published;
        await tenantPlanRepository.UpdateVersionAsync(version, cancellationToken);

        if (request.ExistingTenantPolicy == TenantPlanExistingAssignmentPolicy.MoveExistingTenantsToPublishedVersion)
        {
            IReadOnlyList<TenantPlanAssignment> assignments = await tenantPlanRepository.ListActiveAssignmentsForPlanAsync(
                version.TenantPlanId,
                cancellationToken);

            foreach (TenantPlanAssignment assignment in assignments)
            {
                assignment.TenantPlanVersionId = version.Id;
                assignment.TenantPlanVersion = version;
                await tenantPlanRepository.UpdateAssignmentAsync(assignment, cancellationToken);
            }
        }

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = version.Id,
            Message = "Tenant plan version published."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
