// ABOUTME: Handles runtime updates to tenant policy settings after onboarding.
// ABOUTME: Enforces tenant/instance administrator authorization before applying tenant overrides.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Commands;

public class UpdateTenantPolicySettingsCommandHandler : IRequestHandler<UpdateTenantPolicySettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly ITenantMemberRepository _tenantMemberRepository;
    private readonly IInstanceAdministratorRepository _instanceAdministratorRepository;
    private readonly ITenantPolicySettingService _policySettingService;

    public UpdateTenantPolicySettingsCommandHandler(
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

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateTenantPolicySettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var tenantId = _tenantContext.TenantId;

        if (!await IsUserAuthorizedAsync(tenantId, request.UserId))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can update tenant settings.";
            return response;
        }

        await _policySettingService.ApplyTenantSettingsAsync(tenantId, request.UserId, request.Settings);

        var onboardingState = await _tenantOnboardingStateRepository.GetByTenantId(tenantId);
        response.Id = onboardingState?.Id ?? Guid.Empty;
        response.Success = true;
        response.Message = "Tenant settings updated successfully.";
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
