// ABOUTME: Unlocks a tenant-scoped setting override via the Control Plane write surface.
// ABOUTME: Idempotent: returns success when the override is already unlocked or after applying the unlock.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class UnlockControlPlaneTenantSettingCommandHandler(ITenantSettingRepository repository)
    : IRequestHandler<UnlockControlPlaneTenantSettingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UnlockControlPlaneTenantSettingCommand request,
        CancellationToken cancellationToken)
    {
        bool applied = await repository.UnlockAsync(request.TenantId, request.Key);
        return new BaseCommandResponse<Guid>
        {
            Id = request.TenantId,
            Success = true,
            Message = applied ? "Tenant setting unlocked." : "Tenant setting already unlocked or no override present.",
        };
    }
}
