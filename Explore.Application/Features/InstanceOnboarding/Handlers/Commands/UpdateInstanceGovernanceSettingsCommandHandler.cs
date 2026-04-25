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
using static Explore.Application.Responses.FailureCodes;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateInstanceGovernanceSettingsCommandHandler : IRequestHandler<UpdateInstanceGovernanceSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInstanceGovernanceSettingService _governanceSettingService;
    private readonly IDeploymentModeProvider _deploymentModeProvider;
    private readonly ILogger<UpdateInstanceGovernanceSettingsCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateInstanceGovernanceSettingsCommandHandler(
        IAdminContext adminContext,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantRepository tenantRepository,
        IInstanceGovernanceSettingService governanceSettingService,
        IDeploymentModeProvider deploymentModeProvider,
        ILogger<UpdateInstanceGovernanceSettingsCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _adminContext = adminContext;
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _tenantRepository = tenantRepository;
        _governanceSettingService = governanceSettingService;
        _deploymentModeProvider = deploymentModeProvider;
        _logger = logger;
        _unitOfWork = unitOfWork;
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
        var isSingleTenant = deploymentMode == DeploymentMode.SingleTenant;
        var isRevertingToSingleTenant = string.Equals(currentMode, "MultiTenant", StringComparison.OrdinalIgnoreCase)
                                        && isSingleTenant;

        LogOnboardingGuardrailRejectionIfNeeded(request.UserId, request.Settings.RenderPolicy);

        // Atomic: validate active-tenant count + persist settings + update bootstrap — all in one transaction.
        // The count check MUST be inside the transaction so validation and write cannot be separated by
        // a concurrent admin action that activates a second tenant between check and commit.
        string? failureCode = null;
        string? failureMessage = null;
        List<string>? failureErrors = null;

        var bootstrapId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (isRevertingToSingleTenant)
            {
                var activeTenantCount = await _tenantRepository.GetActiveTenantCountAsync();
                if (activeTenantCount > 1)
                {
                    failureCode = FailureCodes.DeploymentModeChangeBlockedByActiveTenants;
                    failureMessage = "Cannot revert to Single-Tenant mode.";
                    failureErrors =
                    [
                        $"You have {activeTenantCount} active tenants. Archive or suspend {activeTenantCount - 1} tenant(s) before switching to Single-Tenant mode."
                    ];
                    return Guid.Empty;
                }
            }

            Guid? defaultTenantId = null;
            if (isSingleTenant)
            {
                var defaultTenant = await EnsureDefaultTenantAsync();
                defaultTenantId = defaultTenant.Id;
            }

            await _governanceSettingService.ApplySettingsAsync(defaultTenantId, request.Settings, request.UserId);

            if (bootstrap != null)
            {
                bootstrap.SelectedDeploymentMode = deploymentMode.ToString();
                await _instanceBootstrapStateRepository.Update(bootstrap);
                return bootstrap.Id;
            }

            return Guid.Empty;
        }, cancellationToken);

        if (failureCode is not null)
        {
            response.Success = false;
            response.FailureCode = failureCode;
            response.Message = failureMessage;
            response.Errors = failureErrors;
            return response;
        }

        // Invalidate the cached deployment mode so all in-process caches reflect the new value immediately.
        await _deploymentModeProvider.InvalidateCacheAsync();

        response.Id = bootstrapId;
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
            TenantStatus = null!
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
