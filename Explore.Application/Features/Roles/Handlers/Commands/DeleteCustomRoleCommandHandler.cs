// ABOUTME: Handler for deleting custom (non-system) roles with active member validation.
// ABOUTME: Prevents deletion of roles with active members, removes permissions, triggers PolicySync.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Roles.Requests.Commands;
using Explore.Application.Responses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Roles.Handlers.Commands;

public class DeleteCustomRoleCommandHandler : IRequestHandler<DeleteCustomRoleCommand, BaseCommandResponse<int>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICapabilityCeilingService _capabilityCeiling;
    private readonly IPolicySyncService _policySyncService;
    private readonly IPermissionRegistryService _permissionRegistry;
    private readonly ILogger<DeleteCustomRoleCommandHandler> _logger;

    public DeleteCustomRoleCommandHandler(
        IRoleRepository roleRepository,
        ICapabilityCeilingService capabilityCeiling,
        IPolicySyncService policySyncService,
        IPermissionRegistryService permissionRegistry,
        ILogger<DeleteCustomRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _capabilityCeiling = capabilityCeiling;
        _policySyncService = policySyncService;
        _permissionRegistry = permissionRegistry;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<int>> Handle(DeleteCustomRoleCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<int>();

        // Rule 4: System immutability check
        var modCheck = await _capabilityCeiling.CanModifyRoleAsync(request.RoleId);
        if (!modCheck.IsAllowed)
        {
            response.Success = false;
            response.Message = modCheck.Error!;
            return response;
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            response.Success = false;
            response.Message = "Role not found.";
            return response;
        }

        // Check for active members
        var hasMembers = await _roleRepository.HasActiveMembersAsync(request.RoleId);
        if (hasMembers)
        {
            response.Success = false;
            response.Message = $"Cannot delete role '{role.FullName}'. It still has active members assigned.";
            return response;
        }

        // Remove all permissions first (RolePermission entries)
        await _roleRepository.RemoveAllPermissionsAsync(request.RoleId);

        // Delete the role (hard delete since custom roles aren't soft-deletable)
        await _roleRepository.HardDelete(role);

        // Trigger Cerbos policy sync to remove policies for this role
        try
        {
            await _policySyncService.SyncRolePoliciesAsync(request.RoleId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Policy sync failed after deleting role {RoleId}, will retry on next sync", request.RoleId);
        }

        // Invalidate permission cache
        _permissionRegistry.InvalidateCache();

        response.Success = true;
        response.Id = request.RoleId;
        response.Message = $"Custom role '{role.FullName}' deleted.";

        return response;
    }
}
