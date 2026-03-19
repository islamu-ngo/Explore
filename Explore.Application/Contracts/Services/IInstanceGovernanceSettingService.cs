// ABOUTME: Service contract for reading and applying instance-level governance settings.
// ABOUTME: Uses focused sub-resource DTOs instead of a monolithic god object.

using Explore.Application.DTOs.Instance;

namespace Explore.Application.Contracts.Services;

public interface IInstanceGovernanceSettingService
{
    Task<InstanceGovernanceSettings> ReadSettingsAsync();

    Task<InstanceGovernanceSettings> ReadEffectiveSettingsForTenantAsync(Guid tenantId);

    Task ApplySettingsAsync(Guid? defaultTenantId, InstanceGovernanceSettings settings, Guid? actorUserId);

    Task ApplyModuleSettingsAsync(Guid? defaultTenantId, ModuleSettingsDto modules, Guid? actorUserId);

    Task ApplyEventPolicyAsync(EventPolicyDto eventPolicy, Guid? actorUserId);

    Task ApplyOrganizationPolicyAsync(OrganizationPolicyDto orgPolicy, Guid? actorUserId);

    Task ApplyBrandingSettingsAsync(BrandingSettingsDto branding, Guid? actorUserId);

    Task ApplyDomainSettingsAsync(DomainSettingsDto domains, Guid? actorUserId);

    Task ApplyTenantDelegationSettingsAsync(TenantDelegationSettingsDto delegation, Guid? actorUserId);

    Task ApplyRenderPolicySettingsAsync(RenderPolicySettingsDto renderPolicy, Guid? actorUserId);
}
