// ABOUTME: Handler for creating custom roles with capability ceiling enforcement.
// ABOUTME: Generates MasterCode from scope+name, assigns permissions, triggers PolicySync.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Roles.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Roles.Handlers.Commands;

public class CreateCustomRoleCommandHandler : IRequestHandler<CreateCustomRoleCommand, BaseCommandResponse<int>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly ICapabilityCeilingService _capabilityCeiling;
    private readonly IPolicySyncService _policySyncService;
    private readonly IPermissionRegistryService _permissionRegistry;
    private readonly ILogger<CreateCustomRoleCommandHandler> _logger;

    public CreateCustomRoleCommandHandler(
        IRoleRepository roleRepository,
        ICapabilityCeilingService capabilityCeiling,
        IPolicySyncService policySyncService,
        IPermissionRegistryService permissionRegistry,
        ILogger<CreateCustomRoleCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _capabilityCeiling = capabilityCeiling;
        _policySyncService = policySyncService;
        _permissionRegistry = permissionRegistry;
        _logger = logger;
    }

    public async Task<BaseCommandResponse<int>> Handle(CreateCustomRoleCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<int>();

        // Validate name
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            response.Success = false;
            response.Message = "Role name is required.";
            return response;
        }

        // Generate MasterCode from scope prefix + sanitized name
        var scopePrefix = request.Scope switch
        {
            RoleScopeEnum.Platform => "platform",
            RoleScopeEnum.Tenant => "tenant",
            RoleScopeEnum.Organization => "org",
            _ => "custom"
        };
        var masterCode = $"{scopePrefix}.custom.{request.FullName.ToLowerInvariant().Replace(' ', '_')}";

        // Check MasterCode uniqueness
        var existing = await _roleRepository.GetByMasterCodeAsync(masterCode);
        if (existing != null)
        {
            response.Success = false;
            response.Message = $"A role with code '{masterCode}' already exists.";
            return response;
        }

        // Capability ceiling validation
        var ceilingErrors = await _capabilityCeiling.ValidatePermissionAssignmentAsync(
            request.CallerRoleIds,
            request.CallerHighestScope,
            request.PermissionIds,
            request.Scope);

        if (ceilingErrors.Count > 0)
        {
            response.Success = false;
            response.Message = "Permission assignment validation failed.";
            response.Errors = ceilingErrors;
            return response;
        }

        // Create the role
        var role = new Role
        {
            MasterCode = masterCode,
            FullName = request.FullName,
            Description = request.Description,
            Scope = request.Scope,
            IsSystem = false
        };

        role = await _roleRepository.Create(role);

        // Assign permissions via RolePermission
        if (request.PermissionIds.Count > 0)
        {
            await _roleRepository.AssignPermissionsAsync(role.Id, request.PermissionIds);
        }

        // Trigger Cerbos policy sync
        try
        {
            await _policySyncService.SyncRolePoliciesAsync(role.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Policy sync failed for new role {RoleId}, will retry on next sync", role.Id);
        }

        // Invalidate permission cache
        _permissionRegistry.InvalidateCache();

        response.Success = true;
        response.Id = role.Id;
        response.Message = $"Custom role '{role.FullName}' created with {request.PermissionIds.Count} permissions.";

        return response;
    }
}
