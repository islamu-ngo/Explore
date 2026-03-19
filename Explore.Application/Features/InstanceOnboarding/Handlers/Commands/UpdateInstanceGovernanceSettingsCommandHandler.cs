// ABOUTME: Handles runtime instance governance updates by authorized instance administrators.
// ABOUTME: Persists deployment and policy setting changes and keeps default tenant capabilities aligned.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Instance.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateInstanceGovernanceSettingsCommandHandler : IRequestHandler<UpdateInstanceGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;
    private readonly ILogger<UpdateInstanceGovernanceSettingsCommandHandler> _logger;

    public UpdateInstanceGovernanceSettingsCommandHandler(
        IAdminContext adminContext,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantRepository tenantRepository,
        IInstanceGovernanceSettingService governanceSettingService,
        ILogger<UpdateInstanceGovernanceSettingsCommandHandler> logger)
    {
        _adminContext = adminContext;
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _tenantRepository = tenantRepository;
        _governanceSettingService = governanceSettingService;
        _logger = logger;
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

        var deploymentMode = request.Settings.DeploymentMode.Mode;
        if (!Enum.IsDefined(deploymentMode))
        {
            response.Success = false;
            response.Message = "Invalid deployment mode.";
            response.Errors = new List<string> { "DeploymentMode must be SingleTenant or MultiTenant." };
            return response;
        }

        var renderPolicyValidator = new RenderPolicySettingsDtoValidator();
        var renderPolicyValidation = await renderPolicyValidator.ValidateAsync(request.Settings.RenderPolicy, cancellationToken);
        if (!renderPolicyValidation.IsValid)
        {
            response.Success = false;
            response.Message = "Invalid instance governance settings.";
            response.Errors = renderPolicyValidation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
        var currentMode = bootstrap?.SelectedDeploymentMode;

        if (string.Equals(currentMode, "MultiTenant", StringComparison.OrdinalIgnoreCase)
            && deploymentMode == DeploymentMode.SingleTenant)
        {
            var tenantCount = await _tenantRepository.GetActiveTenantCountAsync();
            if (tenantCount > 1)
            {
                response.Success = false;
                response.Message = "Cannot revert to Single-Tenant mode.";
                response.Errors = new List<string>
                {
                    $"You currently have {tenantCount} tenants. Please delete {tenantCount - 1} tenant(s) to enable Single-Tenant mode."
                };
                return response;
            }
        }

        var isSingleTenant = deploymentMode == DeploymentMode.SingleTenant;
        Guid? defaultTenantId = null;

        if (isSingleTenant)
        {
            var defaultTenant = await EnsureDefaultTenantAsync();
            defaultTenantId = defaultTenant.Id;
        }

        LogOnboardingGuardrailRejectionIfNeeded(request.UserId, request.Settings.RenderPolicy);

        await _governanceSettingService.ApplySettingsAsync(defaultTenantId, request.Settings, request.UserId);

        if (bootstrap != null)
        {
            bootstrap.SelectedDeploymentMode = deploymentMode.ToString();
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
        if (tenant != null) return tenant;

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

    private void LogOnboardingGuardrailRejectionIfNeeded(Guid userId, RenderPolicySettingsDto rp)
    {
        var usesInteractiveServerOnOnboarding = string.Equals(
            rp.OnboardingRenderMode,
            RenderModeOptionEnum.InteractiveServer.ToString(),
            StringComparison.OrdinalIgnoreCase);

        if (!usesInteractiveServerOnOnboarding && rp.DisallowInteractiveServerOnOnboarding)
            return;

        _logger.LogWarning(
            "Rejected instance governance update due to onboarding render-policy guardrail violation. UserId: {UserId}, OnboardingRenderMode: {OnboardingRenderMode}, DisallowInteractiveServerOnOnboarding: {DisallowInteractiveServerOnOnboarding}",
            userId, rp.OnboardingRenderMode, rp.DisallowInteractiveServerOnOnboarding);
    }
}
