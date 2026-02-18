// ABOUTME: Handles completion of tenant onboarding by persisting tenant policy choices.
// ABOUTME: Restricts completion to tenant administrators (Owner/Admin) or instance administrators only.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Commands;

public class CompleteTenantOnboardingCommandHandler : IRequestHandler<CompleteTenantOnboardingCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ITenantPolicySettingService _policySettingService;

    public CompleteTenantOnboardingCommandHandler(
        ITenantContext tenantContext,
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        IAdminContext adminContext,
        ITenantPolicySettingService policySettingService)
    {
        _tenantContext = tenantContext;
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _adminContext = adminContext;
        _policySettingService = policySettingService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CompleteTenantOnboardingCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var tenantId = _tenantContext.TenantId;

        if (!await IsUserAuthorizedAsync(tenantId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can complete tenant onboarding.";
            return response;
        }

        await _policySettingService.ApplyTenantSettingsAsync(tenantId, request.UserId, request.Settings);

        var onboardingState = await _tenantOnboardingStateRepository.GetByTenantId(tenantId);
        if (onboardingState == null)
        {
            onboardingState = await _tenantOnboardingStateRepository.Create(new TenantOnboardingState
            {
                TenantId = tenantId,
                Tenant = null!,
                IsCompleted = true,
                CurrentStep = 4,
                TotalSteps = 4,
                CompletedStepsJson = "[\"Identity\",\"Policies\",\"Branding\",\"Review\"]",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CompletedByUserId = request.UserId
            });
        }
        else
        {
            onboardingState.IsCompleted = true;
            onboardingState.CurrentStep = Math.Max(onboardingState.CurrentStep, 4);
            onboardingState.TotalSteps = Math.Max(onboardingState.TotalSteps, 4);
            if (string.IsNullOrWhiteSpace(onboardingState.CompletedStepsJson))
            {
                onboardingState.CompletedStepsJson = "[\"Identity\",\"Policies\",\"Branding\",\"Review\"]";
            }
            onboardingState.CompletedAt = DateTime.UtcNow;
            onboardingState.CompletedByUserId = request.UserId;
            await _tenantOnboardingStateRepository.Update(onboardingState);
        }

        response.Success = true;
        response.Message = "Tenant onboarding completed successfully.";
        response.Id = onboardingState.Id;
        return response;
    }

    private async Task<bool> IsUserAuthorizedAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            return true;
        }

        return await _adminContext.IsInstanceAdminAsync(cancellationToken);
    }
}
