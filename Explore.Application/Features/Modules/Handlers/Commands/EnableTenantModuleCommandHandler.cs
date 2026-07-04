// ABOUTME: Handles tenant module enablement through the application authorization pipeline.
// ABOUTME: Delegates persistence to IModuleService after resolving the current user for audit.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Modules.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Modules.Handlers.Commands;

public sealed class EnableTenantModuleCommandHandler(
    IModuleService moduleService,
    IAdminContext adminContext)
    : IRequestHandler<EnableTenantModuleCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        EnableTenantModuleCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ModuleKey))
        {
            return Failure(request.TenantId, "Module key is required.");
        }

        var enabledBy = await adminContext.ResolveUserIdAsync(cancellationToken);
        var success = await moduleService.EnableModuleAsync(
            request.TenantId,
            request.ModuleKey,
            enabledBy,
            cancellationToken);

        return success
            ? new BaseCommandResponse<Guid>
            {
                Id = request.TenantId,
                Success = true,
                Message = "Module enabled."
            }
            : Failure(request.TenantId, $"Module '{request.ModuleKey}' not found or not active.");
    }

    private static BaseCommandResponse<Guid> Failure(Guid tenantId, string message) => new()
    {
        Id = tenantId,
        Success = false,
        Message = message,
        Errors = [message]
    };
}
