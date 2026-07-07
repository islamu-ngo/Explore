// ABOUTME: Command handler for replacing a draft tenant plan version's template rows.
// ABOUTME: Validates draft content before replacing normalized setting and quota rows.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class UpdateControlPlaneTenantPlanVersionDraftCommandHandler(ITenantPlanRepository tenantPlanRepository)
    : IRequestHandler<UpdateControlPlaneTenantPlanVersionDraftCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateControlPlaneTenantPlanVersionDraftCommand request,
        CancellationToken cancellationToken)
    {
        TenantPlanVersion? version = await tenantPlanRepository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version is null)
        {
            return Failure("Tenant plan version was not found.", ["tenant_plan_version_not_found"]);
        }

        if (version.TenantPlanStatusId != (int)TenantPlanStatusEnum.Draft)
        {
            return Failure("Only draft tenant plan versions can be updated.", ["tenant_plan_version_not_draft"]);
        }

        if (!string.Equals(version.TenantPlan.Key, request.Draft.Key, StringComparison.Ordinal))
        {
            return Failure("Tenant plan draft key does not match the target plan.", ["tenant_plan_key_mismatch"]);
        }

        TenantPlanValidationResult validation = TenantPlanDraftValidator.Validate(request.Draft);
        if (!validation.IsValid)
        {
            return Failure("Tenant plan draft is invalid.", validation.Errors.Select(error => error.Code));
        }

        ControlPlaneTenantPlanDraftMapper.ApplyToVersion(version, request.Draft);
        await tenantPlanRepository.ReplaceVersionContentAsync(version, cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = version.Id,
            Message = "Tenant plan draft version updated."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
