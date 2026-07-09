// ABOUTME: Writes a tenant-scoped setting override via the Control Plane write surface.
// ABOUTME: Preserves the existing IsLocked state so override writes do not silently drop tenant locks.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class SetControlPlaneTenantSettingCommandHandler(ITenantSettingRepository repository)
    : IRequestHandler<SetControlPlaneTenantSettingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SetControlPlaneTenantSettingCommand request,
        CancellationToken cancellationToken)
    {
        // ponytail: read-before-write to preserve IsLocked; one extra read beats silently dropping a tenant lock.
        var existing = await repository.GetByTenantAndKey(request.TenantId, request.Key);
        bool preserveLocked = existing?.IsLocked ?? false;

        await repository.UpsertManyForTenantAsync(
            request.TenantId,
            [new TenantSettingOverrideUpsert(request.Key, request.Value, preserveLocked)],
            cancellationToken);

        return new BaseCommandResponse<Guid>
        {
            Id = request.TenantId,
            Success = true,
            Message = "Tenant setting updated.",
        };
    }
}
