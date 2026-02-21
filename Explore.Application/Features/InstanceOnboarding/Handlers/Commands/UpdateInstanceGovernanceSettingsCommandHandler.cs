// ABOUTME: Handles runtime instance governance updates by authorized instance administrators.
// ABOUTME: Persists deployment and policy setting changes and keeps default tenant capabilities aligned.

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

public class UpdateInstanceGovernanceSettingsCommandHandler : IRequestHandler<UpdateInstanceGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantSettingsRepository _tenantSettingsRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;

    public UpdateInstanceGovernanceSettingsCommandHandler(
        IAdminContext adminContext,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantRepository tenantRepository,
        ITenantSettingsRepository tenantSettingsRepository,
        IInstanceGovernanceSettingService governanceSettingService)
    {
        _adminContext = adminContext;
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _tenantRepository = tenantRepository;
        _tenantSettingsRepository = tenantSettingsRepository;
        _governanceSettingService = governanceSettingService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateInstanceGovernanceSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update instance governance settings.";
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

        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
        if (bootstrap != null)
        {
            bootstrap.SelectedDeploymentMode = request.Settings.DeploymentMode;
            await _instanceBootstrapStateRepository.Update(bootstrap);
            response.Id = bootstrap.Id;
        }
        else
        {
            response.Id = Guid.Empty;
        }

        response.Success = true;
        response.Message = "Instance governance settings updated successfully.";
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
