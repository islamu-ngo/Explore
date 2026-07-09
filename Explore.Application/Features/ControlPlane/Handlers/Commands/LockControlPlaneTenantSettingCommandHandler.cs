// ABOUTME: Locks a tenant-scoped setting override via the Control Plane write surface.
// ABOUTME: Idempotent: returns success when the override is already locked or after applying the lock.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class LockControlPlaneTenantSettingCommandHandler(ITenantSettingRepository repository)
    : IRequestHandler<LockControlPlaneTenantSettingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        LockControlPlaneTenantSettingCommand request,
        CancellationToken cancellationToken)
    {
        bool applied = await repository.LockAsync(request.TenantId, request.Key);
        return new BaseCommandResponse<Guid>
        {
            Id = request.TenantId,
            Success = true,
            Message = applied ? "Tenant setting locked." : "Tenant setting already locked or no override present.",
        };
    }
}
