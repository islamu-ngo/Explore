// ABOUTME: Handles tenant user role grant creation with tenant-local lifecycle validation.
// ABOUTME: Creates auditable authority evidence only for active TenantUser records.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantUserRoleGrant.Validators;
using Explore.Application.Features.TenantUserRoleGrants.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.TenantUserRoleGrants.Handlers.Commands;

public class CreateTenantUserRoleGrantCommandHandler : IRequestHandler<CreateTenantUserRoleGrantCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateTenantUserRoleGrantCommandHandler(
        ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
        ITenantUserRepository tenantUserRepository,
        IRoleRepository roleRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _tenantUserRoleGrantRepository = tenantUserRoleGrantRepository;
        _tenantUserRepository = tenantUserRepository;
        _roleRepository = roleRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantUserRoleGrantCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var dto = request.TenantUserRoleGrantDto;

        var validator = new CreateTenantUserRoleGrantDtoValidator(_tenantUserRepository, _roleRepository);
        var validationResult = await validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant user role grant creation failed.";
            response.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var tenantUser = await _tenantUserRepository.GetById(dto.TenantUserId);
        if (tenantUser is null || tenantUser.TenantId != _tenantContext.TenantId)
        {
            response.Success = false;
            response.Message = "Tenant user role grant creation failed.";
            response.Errors = ["Tenant-local user state is required before a role can be granted."];
            return response;
        }

        if (tenantUser.StatusId != (int)TenantUserStatusEnum.Active || tenantUser.IsDeleted)
        {
            response.Success = false;
            response.Message = "Tenant user role grant creation failed.";
            response.Errors = ["Tenant-local user must be active before a role can be granted."];
            return response;
        }

        var existingGrant = await _tenantUserRoleGrantRepository.GetByTenantUserAndRole(
            _tenantContext.TenantId,
            dto.TenantUserId,
            dto.RoleId);

        if (existingGrant is not null)
        {
            response.Success = false;
            response.Message = "Tenant user role grant creation failed.";
            response.Errors = ["An active grant for this tenant user and role already exists."];
            return response;
        }

        var tenantUserRoleGrant = new TenantUserRoleGrant
        {
            TenantId = _tenantContext.TenantId,
            Tenant = null!,
            TenantUserId = tenantUser.Id,
            TenantUser = null!,
            RoleId = dto.RoleId,
            Role = null!,
            RoleScopeId = (int)RoleScopeEnum.Tenant,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = _currentUserService.UserId
        };

        tenantUserRoleGrant = await _tenantUserRoleGrantRepository.Create(tenantUserRoleGrant);

        response.Success = true;
        response.Id = tenantUserRoleGrant.Id;
        response.Message = "Tenant user role grant created successfully.";

        return response;
    }
}
