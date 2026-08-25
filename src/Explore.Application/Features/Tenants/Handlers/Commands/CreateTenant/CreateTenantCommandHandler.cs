// ABOUTME: Handles tenant creation with optional automatic admin assignment for the requesting user.
// ABOUTME: Validates input, enforces slug uniqueness, and atomically creates tenant, branding, and role grants.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.Management;
using Explore.Application.Features.Tenants.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Tenants.Handlers.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantUserRoleGrantRepository _tenantUserRoleGrantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantBrandingSettingsDocumentProvisioningService _tenantBrandingProvisioningService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingMutationLock _mutationLock;
    private readonly TenantActivationCapacityPolicy _capacityPolicy;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        ITenantUserRoleGrantRepository tenantUserRoleGrantRepository,
        ITenantUserRepository tenantUserRepository,
        IRoleRepository roleRepository,
        ITenantBrandingSettingsDocumentProvisioningService tenantBrandingProvisioningService,
        ILogger<CreateTenantCommandHandler> logger,
        IUnitOfWork unitOfWork,
        ISettingMutationLock mutationLock,
        TenantActivationCapacityPolicy capacityPolicy)
    {
        _tenantRepository = tenantRepository;
        _tenantUserRoleGrantRepository = tenantUserRoleGrantRepository;
        _tenantUserRepository = tenantUserRepository;
        _roleRepository = roleRepository;
        _tenantBrandingProvisioningService = tenantBrandingProvisioningService;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _mutationLock = mutationLock;
        _capacityPolicy = capacityPolicy;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var dto = request.TenantDto;

        var validator = new CreateTenantDtoValidator();
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            return BaseCommandResponse.Validation<Guid>(
                validation.Errors.Select(e => e.ErrorMessage),
                "Invalid tenant data.");
        }

        var existingTenant = await _tenantRepository.GetTenantBySlug(dto.Slug);
        if (existingTenant != null)
        {
            return BaseCommandResponse.Validation<Guid>(
                ["Slug must be unique across all tenants."],
                "A tenant with this slug already exists.");
        }

        var statusId = dto.IsActive ? (int)TenantStatusEnum.Active : (int)TenantStatusEnum.Provisioning;
        var assignAdmin = dto.AssignCurrentUserAsTenantAdmin && request.RequestingUserId.HasValue;

        async Task<BaseCommandResponse<Guid>> CreateAsync(CancellationToken ct)
        {
            if (dto.IsActive)
            {
                TenantActivationCapacityAssessment capacity = await _capacityPolicy.EvaluateAsync(
                    requireMultiTenant: false,
                    cancellationToken: ct);
                if (!capacity.Allowed)
                {
                    return BaseCommandResponse.Failure<Guid>(
                        capacity.FailureCode!,
                        capacity.Error,
                        [capacity.Error!]);
                }
            }

            var tenant = await _tenantRepository.Create(new Tenant
            {
                FullName = dto.FullName,
                Slug = dto.Slug,
                TenantStatusId = statusId,
                TenantStatus = null!
            });

            await _tenantBrandingProvisioningService.EnsureTenantBrandingDocumentAsync(tenant.Id, dto.FullName, ct);

            if (assignAdmin)
            {
                await AssignRequestingUserAsTenantAdminAsync(tenant.Id, request.RequestingUserId!.Value, ct);
            }

            return BaseCommandResponse.Success(
                tenant.Id,
                assignAdmin
                    ? "Tenant created successfully. You have been assigned as tenant administrator."
                    : "Tenant created successfully.");
        }

        return _capacityPolicy.IsEnforced
            ? await _mutationLock.ExecuteAsync(
                GovernanceSettingKeys.Deployment.Mode,
                CreateAsync,
                cancellationToken)
            : await _unitOfWork.ExecuteInTransactionAsync(CreateAsync, cancellationToken);
    }

    private async Task AssignRequestingUserAsTenantAdminAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var tenantAdminRole = await _roleRepository.GetByMasterCodeAsync("tenant.admin")
            ?? await _roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin);

        if (tenantAdminRole == null)
        {
            _logger.LogWarning(
                "tenant.admin role not found; skipping automatic admin assignment for UserId={UserId} on TenantId={TenantId}.",
                userId, tenantId);
            return;
        }

        var existing = await _tenantUserRoleGrantRepository.GetByTenantAndUser(tenantId, userId);
        if (existing != null) return;

        var tenantUser = await _tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, ct)
            ?? await _tenantUserRepository.Create(new TenantUser
            {
                TenantId = tenantId,
                Tenant = null!,
                UserId = userId,
                User = null!,
                StatusId = (int)TenantUserStatusEnum.Active,
                JoinedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });

        await _tenantUserRoleGrantRepository.Create(new TenantUserRoleGrant
        {
            TenantId = tenantId,
            Tenant = null!,
            TenantUserId = tenantUser.Id,
            TenantUser = null!,
            RoleId = tenantAdminRole.Id,
            Role = null!,
            RoleScopeId = (int)RoleScopeEnum.Tenant,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
    }
}
