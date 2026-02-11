// ABOUTME: Resolves effective public experience settings through system->tenant cascade for current tenant context.
// ABOUTME: Supports anonymous-safe home page routing and white-label branding consumption.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Domain.Constants;
using MediatR;

namespace Explore.Application.Features.PublicExperience.Handlers.Queries;

public class GetPublicExperienceSettingsQueryHandler : IRequestHandler<GetPublicExperienceSettingsQuery, PublicExperienceSettingsDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ITenantPolicySettingService _policySettingService;

    public GetPublicExperienceSettingsQueryHandler(
        ITenantContext tenantContext,
        ISystemSettingRepository systemSettingRepository,
        ITenantPolicySettingService policySettingService)
    {
        _tenantContext = tenantContext;
        _systemSettingRepository = systemSettingRepository;
        _policySettingService = policySettingService;
    }

    public async Task<PublicExperienceSettingsDto> Handle(GetPublicExperienceSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var effectiveTenantSettings = await _policySettingService.ReadEffectiveTenantSettingsAsync(tenantId);

        var deploymentModeSetting = await _systemSettingRepository.GetByKey(GovernanceSettingKeys.DeploymentMode);
        var deploymentMode = DeserializeString(deploymentModeSetting?.Value, "SingleTenant");

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

    private static string DeserializeString(string? rawValue, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        try
        {
            var deserialized = JsonSerializer.Deserialize<string>(rawValue);
            return string.IsNullOrWhiteSpace(deserialized) ? defaultValue : deserialized;
        }
        catch
        {
            return rawValue.Trim('"');
        }
    }
}
