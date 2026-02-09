// ABOUTME: Handles tenant onboarding status queries for startup flow routing decisions.
// ABOUTME: Combines tenant onboarding completion state with current user's tenant/instance admin eligibility.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.TenantOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Queries;

public class GetTenantOnboardingStatusQueryHandler : IRequestHandler<GetTenantOnboardingStatusQuery, TenantOnboardingStatusDto>
{
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly ITenantAdministratorRepository _tenantAdministratorRepository;
    private readonly IInstanceAdministratorRepository _instanceAdministratorRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public GetTenantOnboardingStatusQueryHandler(
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        ITenantAdministratorRepository tenantAdministratorRepository,
        IInstanceAdministratorRepository instanceAdministratorRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _tenantAdministratorRepository = tenantAdministratorRepository;
        _instanceAdministratorRepository = instanceAdministratorRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public async Task<TenantOnboardingStatusDto> Handle(GetTenantOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var onboardingState = await _tenantOnboardingStateRepository.GetByTenantId(tenantId);

        var response = new TenantOnboardingStatusDto
        {
            TenantId = tenantId,
            IsCompleted = onboardingState?.IsCompleted == true,
            IsAuthenticated = _currentUserService.IsAuthenticated,
            IsCurrentUserTenantAdministrator = false,
            IsCurrentUserInstanceAdministrator = false
        };

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return response;
        }

        var userId = _currentUserService.UserId.Value;
        response.IsCurrentUserTenantAdministrator = await _tenantAdministratorRepository.IsTenantAdministrator(tenantId, userId);
        response.IsCurrentUserInstanceAdministrator = await _instanceAdministratorRepository.IsUserInstanceAdmin(userId);

        return response;
    }
}
