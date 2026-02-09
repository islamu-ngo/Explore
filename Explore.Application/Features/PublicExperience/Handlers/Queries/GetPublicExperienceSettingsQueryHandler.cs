// ABOUTME: Resolves effective public experience settings through system->tenant cascade for current tenant context.
// ABOUTME: Supports anonymous-safe home page routing and white-label branding consumption.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Common;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Features.TenantOnboarding.Common;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Handlers.Queries;

public class GetPublicExperienceSettingsQueryHandler : IRequestHandler<GetPublicExperienceSettingsQuery, PublicExperienceSettingsDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantSettingRepository _tenantSettingRepository;
    private readonly ITenantRepository _tenantRepository;

    public GetPublicExperienceSettingsQueryHandler(
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

    public async Task<PublicExperienceSettingsDto> Handle(GetPublicExperienceSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var effectiveTenantSettings = await TenantPolicySettingHelpers.ReadEffectiveTenantSettingsAsync(
            _systemSettingRepository,
            _tenantSettingRepository,
            _tenantRepository,
            tenantId);

        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var deploymentMode = InstanceGovernanceSettingHelpers.DeserializeString(deploymentModeSetting?.Value, "SingleTenant");

        return new PublicExperienceSettingsDto
        {
            TenantId = tenantId,
            DeploymentMode = deploymentMode,
            PreferredHomePage = effectiveTenantSettings.PreferredHomePage,
            BrandDisplayName = effectiveTenantSettings.BrandDisplayName,
            BrandLogoUrl = effectiveTenantSettings.BrandLogoUrl,
            BrandFaviconUrl = effectiveTenantSettings.BrandFaviconUrl,
            BrandCustomCssUrl = effectiveTenantSettings.BrandCustomCssUrl,
            InstanceBaseDomain = effectiveTenantSettings.InstanceBaseDomain,
            Subdomain = effectiveTenantSettings.Subdomain,
            CustomDomain = effectiveTenantSettings.CustomDomain
        };
    }
}
