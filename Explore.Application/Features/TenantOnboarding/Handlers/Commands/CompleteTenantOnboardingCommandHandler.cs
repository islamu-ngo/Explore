// ABOUTME: Handles completion of tenant onboarding by persisting tenant policy choices.
// ABOUTME: Restricts completion to tenant administrators or instance administrators for the current tenant.

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
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IInstanceAdministratorRepository _instanceAdministratorRepository;
    private readonly ITenantPolicySettingService _policySettingService;

    public CompleteTenantOnboardingCommandHandler(
        ITenantContext tenantContext,
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        ITenantMemberRepository tenantMemberRepository,
        IInstanceAdministratorRepository instanceAdministratorRepository,
        ITenantPolicySettingService policySettingService)
    {
        _tenantContext = tenantContext;
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _tenantMemberRepository = tenantMemberRepository;
        _instanceAdministratorRepository = instanceAdministratorRepository;
        _policySettingService = policySettingService;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(CompleteTenantOnboardingCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var tenantId = _tenantContext.TenantId;

        if (!await IsUserAuthorizedAsync(tenantId, request.UserId))
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
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CompletedByUserId = request.UserId
            });
        }
        else
        {
            onboardingState.IsCompleted = true;
            onboardingState.CompletedAt = DateTime.UtcNow;
            onboardingState.CompletedByUserId = request.UserId;
            await _tenantOnboardingStateRepository.Update(onboardingState);
        }

        response.Success = true;
        response.Message = "Tenant onboarding completed successfully.";
        response.Id = onboardingState.Id;
        return response;
    }

    private async Task<bool> IsUserAuthorizedAsync(Guid tenantId, Guid userId)
    {
        if (await _tenantMemberRepository.IsTenantMember(tenantId, userId))
        {
            return true;
        }

        return await _instanceAdministratorRepository.IsUserInstanceAdmin(userId);
    }
}
