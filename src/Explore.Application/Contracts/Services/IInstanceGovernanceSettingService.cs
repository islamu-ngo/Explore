// ABOUTME: Service contract for reading and applying instance-level governance settings.
// ABOUTME: Uses focused sub-resource DTOs instead of a monolithic god object.

using Explore.Application.DTOs.Instance;
using Explore.Application.Notifications;

namespace Explore.Application.Contracts.Services;

public interface IInstanceGovernanceSettingService
{
    Task<InstanceGovernanceSettings> ReadSettingsAsync();

    Task<InstanceGovernanceSettings> ReadEffectiveSettingsForTenantAsync(Guid tenantId);

    Task<InstanceGovernanceSettingApplyResult> ApplySettingsAsync(
        Guid? defaultTenantId,
        InstanceGovernanceSettings settings,
        Guid? actorUserId);

    Task ApplyModuleSettingsAsync(Guid? defaultTenantId, ModuleSettingsDto modules, Guid? actorUserId);

    Task ApplyEventPolicyAsync(EventPolicyDto eventPolicy, Guid? actorUserId);

    Task ApplyOrganizationPolicyAsync(OrganizationPolicyDto orgPolicy, Guid? actorUserId);

    Task ApplyBrandingSettingsAsync(BrandingSettingsDto branding, Guid? actorUserId);

    Task ApplyDomainSettingsAsync(DomainSettingsDto domains, Guid? actorUserId);
    Task ApplyTenantDelegationSettingsAsync(TenantDelegationSettingsDto delegation, Guid? actorUserId);
    Task ApplyAdminPortalSettingsAsync(AdminPortalSettingsDto adminPortal, Guid? actorUserId);
    Task ApplyAiAssistantGovernanceSettingsAsync(AiAssistantGovernanceSettingsDto aiAssistant, Guid? actorUserId);

    Task ApplyMcpGovernanceSettingsAsync(McpGovernanceSettingsDto mcp, Guid? actorUserId);

    Task ApplyRenderPolicySettingsAsync(RenderPolicySettingsDto renderPolicy, Guid? actorUserId);
}

public sealed record InstanceGovernanceSettingApplyResult(
    IReadOnlyList<LocationPrivacyGovernanceMutationResult> LocationPrivacyMutations,
    IReadOnlyList<SettingChangedNotification> DeferredNotifications);
