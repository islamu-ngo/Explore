// ABOUTME: Handles completion of tenant onboarding by persisting tenant policy choices.
// ABOUTME: Restricts completion to tenant administrators or instance administrators for the current tenant.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.TenantOnboarding.Common;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Commands;

public class CompleteTenantOnboardingCommandHandler : IRequestHandler<CompleteTenantOnboardingCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly ITenantAdministratorRepository _tenantAdministratorRepository;
    private readonly IInstanceAdministratorRepository _instanceAdministratorRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantRepository _tenantRepository;

    public CompleteTenantOnboardingCommandHandler(
        ITenantContext tenantContext,
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        ITenantAdministratorRepository tenantAdministratorRepository,
        IInstanceAdministratorRepository instanceAdministratorRepository,
        ITenantSettingRepository tenantSettingRepository,
        ISystemSettingRepository systemSettingRepository,
        ITenantRepository tenantRepository)
    {
        _tenantContext = tenantContext;
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _tenantAdministratorRepository = tenantAdministratorRepository;
        _instanceAdministratorRepository = instanceAdministratorRepository;
        _tenantSettingRepository = tenantSettingRepository;
        _systemSettingRepository = systemSettingRepository;
        _tenantRepository = tenantRepository;
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

        await TenantPolicySettingHelpers.ApplyTenantSettingsAsync(
            _tenantSettingRepository,
            _systemSettingRepository,
            _tenantRepository,
            tenantId,
            request.UserId,
            request.Settings);

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
        if (await _tenantAdministratorRepository.IsTenantAdministrator(tenantId, userId))
        {
            return true;
        }

        return await _instanceAdministratorRepository.IsUserInstanceAdmin(userId);
    }
}
