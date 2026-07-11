// ABOUTME: Command handler for archiving a tenant plan version template.
// ABOUTME: Stops future provisioning from the version without moving existing assignments.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class ArchiveControlPlaneTenantPlanVersionCommandHandler(ITenantPlanRepository tenantPlanRepository)
    : IRequestHandler<ArchiveControlPlaneTenantPlanVersionCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ArchiveControlPlaneTenantPlanVersionCommand request,
        CancellationToken cancellationToken)
    {
        TenantPlanVersion? version = await tenantPlanRepository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version is null)
        {
            return Failure("Tenant plan version was not found.", ["tenant_plan_version_not_found"]);
        }

        version.TenantPlanStatusId = (int)TenantPlanStatusEnum.Archived;
        version.IsActiveForProvisioning = false;
        await tenantPlanRepository.UpdateVersionAsync(version, cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = version.Id,
            Message = "Tenant plan version archived."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
