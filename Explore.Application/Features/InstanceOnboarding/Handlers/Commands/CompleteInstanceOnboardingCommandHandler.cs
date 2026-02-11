// ABOUTME: Handles first-run instance onboarding completion, role assignment, and governance persistence.
// ABOUTME: Establishes the first instance admin and default tenant admin mapping for a clean database.

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
    private readonly IInstanceAdministratorRepository _instanceAdministratorRepository;
    private readonly ITenantAdministratorRepository _tenantAdministratorRepository;
    private readonly ITenantAdministratorRoleRepository _tenantAdministratorRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;

    public CompleteInstanceOnboardingCommandHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IInstanceAdministratorRepository instanceAdministratorRepository,
        ITenantAdministratorRepository tenantAdministratorRepository,
        ITenantAdministratorRoleRepository tenantAdministratorRoleRepository,
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITenantSettingsRepository tenantSettingsRepository,
        IInstanceGovernanceSettingService governanceSettingService)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _instanceAdministratorRepository = instanceAdministratorRepository;
        _tenantAdministratorRepository = tenantAdministratorRepository;
        _tenantAdministratorRoleRepository = tenantAdministratorRoleRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tenantSettingsRepository = tenantSettingsRepository;
        _governanceSettingService = governanceSettingService;
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
            response.Errors = new List<string> { "Call /api/v1/User/sync before completing onboarding." };
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

        await EnsureInstanceAdministratorAsync(request.UserId);
        await EnsureDefaultTenantAdministratorAsync(defaultTenant.Id, request.UserId);

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
            IsActive = true
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

    private async Task EnsureInstanceAdministratorAsync(Guid userId)
    {
        var existing = await _instanceAdministratorRepository.GetByUserId(userId);
        if (existing != null)
        {
            return;
        }

        await _instanceAdministratorRepository.Create(new InstanceAdministrator
        {
            UserId = userId,
            User = null!,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = userId
        });
    }

    private async Task EnsureDefaultTenantAdministratorAsync(Guid tenantId, Guid userId)
    {
        var tenantAdmin = await _tenantAdministratorRepository.GetByTenantAndUser(tenantId, userId);
        var tenantAdminRole = await _tenantAdministratorRoleRepository.GetByMasterCode("TENANT_ADMIN")
            ?? await _tenantAdministratorRoleRepository.GetById((int)TenantAdministratorRoleEnum.TenantAdmin);

        if (tenantAdminRole == null)
        {
            return;
        }

        if (tenantAdmin == null)
        {
            await _tenantAdministratorRepository.Create(new TenantAdministrator
            {
                TenantId = tenantId,
                Tenant = null!,
                UserId = userId,
                User = null!,
                TenantAdministratorRoleId = tenantAdminRole.Id,
                TenantAdministratorRole = null!,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = userId
            });

            return;
        }

        if (tenantAdmin.TenantAdministratorRoleId == tenantAdminRole.Id)
        {
            return;
        }

        tenantAdmin.TenantAdministratorRoleId = tenantAdminRole.Id;
        await _tenantAdministratorRepository.Update(tenantAdmin);
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
