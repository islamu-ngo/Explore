// ABOUTME: Handles tenant onboarding status queries for startup flow routing decisions.
// ABOUTME: Combines tenant onboarding completion state with current user's tenant/instance admin eligibility.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.TenantOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Queries;

public class GetTenantOnboardingStatusQueryHandler : IRequestHandler<GetTenantOnboardingStatusQuery, TenantOnboardingStatusDto>
{
    private readonly ITenantOnboardingStateRepository _tenantOnboardingStateRepository;
    private readonly IAdminContext _adminContext;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public GetTenantOnboardingStatusQueryHandler(
        ITenantOnboardingStateRepository tenantOnboardingStateRepository,
        IAdminContext adminContext,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _tenantOnboardingStateRepository = tenantOnboardingStateRepository;
        _adminContext = adminContext;
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
            IsCurrentUserPlatformAdministrator = false
        };

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return response;
        }

        response.IsCurrentUserTenantAdministrator = await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken);
        response.IsCurrentUserPlatformAdministrator = await _adminContext.IsInstanceAdminAsync(cancellationToken);

        return response;
    }
}
