// ABOUTME: Handles first-run instance onboarding completion, role assignment, and governance persistence.
// ABOUTME: Establishes the first instance admin and default tenant admin mapping for a clean database.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using TenantSettingsEntity = Explore.Domain.TenantSettings;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class CompleteInstanceOnboardingCommandHandler : IRequestHandler<CompleteInstanceOnboardingCommand, BaseCommandResponse<Guid>>
{
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;
    private readonly ISetupSecretProvider _setupSecretProvider;
    private readonly IAdminCacheInvalidator _adminCacheInvalidator;

    public CompleteInstanceOnboardingCommandHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IUserRoleRepository userRoleRepository,
        ITenantMemberRepository tenantMemberRepository,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITenantSettingsRepository tenantSettingsRepository,
        IInstanceGovernanceSettingService governanceSettingService,
        ISetupSecretProvider setupSecretProvider,
        IAdminCacheInvalidator adminCacheInvalidator)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _userRoleRepository = userRoleRepository;
        _tenantMemberRepository = tenantMemberRepository;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tenantSettingsRepository = tenantSettingsRepository;
        _governanceSettingService = governanceSettingService;
        _setupSecretProvider = setupSecretProvider;
        _adminCacheInvalidator = adminCacheInvalidator;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CompleteInstanceOnboardingCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
        if (bootstrap?.IsCompleted == true)
        {
            response.Success = false;
            response.Message = "Instance onboarding has already been completed.";
            return response;
        }

        var user = await _userRepository.GetById(request.UserId);
        if (user == null)
        {
            response.Success = false;
            response.Message = "Current user is not synchronized in the local database.";
            response.Errors = new List<string> { "Call /api/User/sync before completing onboarding." };
            return response;
        }

        var normalizedDeploymentMode = NormalizeDeploymentMode(request.Settings.DeploymentMode);
        if (normalizedDeploymentMode == null)
        {
            response.Success = false;
            response.Message = "Invalid deployment mode.";
            response.Errors = new List<string> { "DeploymentMode must be SingleTenant or MultiTenant." };
            return response;
        }

        request.Settings.DeploymentMode = normalizedDeploymentMode;

        var defaultTenant = await EnsureDefaultTenantAsync();
        await EnsureDefaultTenantSettingsAsync(defaultTenant.Id);

        await _governanceSettingService.ApplySettingsAsync(defaultTenant.Id, request.Settings, request.UserId);

        await EnsurePlatformAdministratorRoleAsync(request.UserId);
        await EnsureDefaultTenantAdministratorAsync(defaultTenant.Id, request.UserId);

        // Invalidate cached admin status so the new roles are recognized immediately
        // without waiting for the 5-minute sliding cache expiration.
        _adminCacheInvalidator.InvalidateUser(request.UserId);

        if (bootstrap == null)
        {
            bootstrap = await _instanceBootstrapStateRepository.Create(new InstanceBootstrapState
            {
                IsCompleted = true,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CompletedByUserId = request.UserId,
                SelectedDeploymentMode = request.Settings.DeploymentMode
            });
        }
        else
        {
            bootstrap.IsCompleted = true;
            bootstrap.CompletedAt = DateTime.UtcNow;
            bootstrap.CompletedByUserId = request.UserId;
            bootstrap.SelectedDeploymentMode = request.Settings.DeploymentMode;
            await _instanceBootstrapStateRepository.Update(bootstrap);
        }

        // Lock the setup secret provider to prevent further setup mode access.
        // Once locked, all setup-gated endpoints return 410 Gone.
        _setupSecretProvider.Lock();

        response.Success = true;
        response.Message = "Instance onboarding completed successfully.";
        response.Id = bootstrap.Id;
        return response;
    }

    private async Task<Tenant> EnsureDefaultTenantAsync()
    {
        var tenant = await _tenantRepository.GetById(PlatformDefaults.DefaultTenantId);
        if (tenant != null)
        {
            return tenant;
        }

        return await _tenantRepository.Create(new Tenant
        {
            Id = PlatformDefaults.DefaultTenantId,
            FullName = PlatformDefaults.DefaultTenantName,
            Slug = PlatformDefaults.DefaultTenantSlug,
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = new TenantStatus
            {
                Id = (int)TenantStatusEnum.Active,
                MasterCode = "ACTIVE",
                FullName = "Active",
                IsActiveState = true
            }
        });
    }

    private async Task EnsureDefaultTenantSettingsAsync(Guid tenantId)
    {
        var existing = await _tenantSettingsRepository.GetByTenant(tenantId);
        if (existing != null)
        {
            return;
        }

        await _tenantSettingsRepository.Create(new TenantSettingsEntity
        {
            TenantId = tenantId,
            Tenant = null!
        });
    }

    private async Task EnsurePlatformAdministratorRoleAsync(Guid userId)
    {
        var platformAdminRole = await _roleRepository.GetByMasterCodeAsync("platform.admin")
            ?? await _roleRepository.GetByIdAsync((int)RoleEnum.Admin);

        if (platformAdminRole == null || platformAdminRole.Scope != RoleScopeEnum.Platform)
        {
            return;
        }

        var existing = await _userRoleRepository.GetByUserAndRole(userId, platformAdminRole.Id);
        if (existing != null)
        {
            return;
        }

        await _userRoleRepository.Create(new UserRole
        {
            UserId = userId,
            User = null!,
            RoleId = platformAdminRole.Id,
            Role = null!,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
    }

    private async Task EnsureDefaultTenantAdministratorAsync(Guid tenantId, Guid userId)
    {
        var tenantMember = await _tenantMemberRepository.GetByTenantAndUser(tenantId, userId);
        var tenantAdminRole = await _roleRepository.GetByMasterCodeAsync("tenant.admin")
            ?? await _roleRepository.GetByIdAsync((int)RoleEnum.TenantAdmin);

        if (tenantAdminRole == null)
        {
            return;
        }

        if (tenantMember == null)
        {
            await _tenantMemberRepository.Create(new TenantMember
            {
                TenantId = tenantId,
                Tenant = null!,
                UserId = userId,
                User = null!,
                RoleId = tenantAdminRole.Id,
                Role = null!,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = userId
            });

            return;
        }

        if (tenantMember.RoleId == tenantAdminRole.Id)
        {
            return;
        }

        tenantMember.RoleId = tenantAdminRole.Id;
        await _tenantMemberRepository.Update(tenantMember);
    }

    private static string? NormalizeDeploymentMode(string? deploymentMode)
    {
        if (string.IsNullOrWhiteSpace(deploymentMode))
        {
            return null;
        }

        if (deploymentMode.Equals("SingleTenant", StringComparison.OrdinalIgnoreCase))
        {
            return "SingleTenant";
        }

        if (deploymentMode.Equals("MultiTenant", StringComparison.OrdinalIgnoreCase))
        {
            return "MultiTenant";
        }

        return null;
    }
}
