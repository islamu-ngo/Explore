// ABOUTME: Command handler for creating draft control-plane SaaS tenant plans.
// ABOUTME: Validates pricing, settings, and quotas before persisting normalized plan entities.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class CreateControlPlaneTenantPlanDraftCommandHandler(ITenantPlanRepository tenantPlanRepository)
    : IRequestHandler<CreateControlPlaneTenantPlanDraftCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        CreateControlPlaneTenantPlanDraftCommand request,
        CancellationToken cancellationToken)
    {
        TenantPlanValidationResult validation = TenantPlanDraftValidator.Validate(request.Draft);
        if (!validation.IsValid)
        {
            return Failure("Tenant plan draft is invalid.", validation.Errors.Select(error => error.Code));
        }

        var existing = await tenantPlanRepository.GetByKeyAsync(request.Draft.Key, cancellationToken);
        if (existing is not null)
        {
            return Failure("A tenant plan with this key already exists.", ["tenant_plan_key_exists"]);
        }

        var plan = ControlPlaneTenantPlanDraftMapper.ToPlan(request.Draft);
        await tenantPlanRepository.Create(plan);

        return new BaseCommandResponse<Guid>
        {
            Success = true,
            Id = plan.Id,
            Message = "Tenant plan draft created."
        };
    }

    private static BaseCommandResponse<Guid> Failure(string message, IEnumerable<string> errors) => new()
    {
        Success = false,
        Message = message,
        Errors = errors.ToList()
    };
}
