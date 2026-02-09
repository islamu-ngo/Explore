// ABOUTME: Handles queries for effective tenant policy settings used in tenant onboarding questionnaires.
// ABOUTME: Resolves tenant overrides against instance defaults and delegation constraints.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.TenantOnboarding.Common;
using Explore.Application.Features.TenantOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.TenantOnboarding.Handlers.Queries;

public class GetTenantPolicySettingsQueryHandler : IRequestHandler<GetTenantPolicySettingsQuery, TenantPolicySettingsDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly ITenantRepository _tenantRepository;

    public GetTenantPolicySettingsQueryHandler(
        ITenantContext tenantContext,
        ISystemSettingRepository systemSettingRepository,
        ITenantSettingRepository tenantSettingRepository,
        ITenantRepository tenantRepository)
    {
        _tenantContext = tenantContext;
        _systemSettingRepository = systemSettingRepository;
        _tenantSettingRepository = tenantSettingRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantPolicySettingsDto> Handle(GetTenantPolicySettingsQuery request, CancellationToken cancellationToken)
    {
        return await TenantPolicySettingHelpers.ReadEffectiveTenantSettingsAsync(
            _systemSettingRepository,
            _tenantSettingRepository,
            _tenantRepository,
            _tenantContext.TenantId);
    }
}
