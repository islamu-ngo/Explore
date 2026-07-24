// ABOUTME: Presence-aware write DTOs for ordinary instance governance sub-resources.
// ABOUTME: Each contract distinguishes omitted leaves from explicit values without reusing read DTOs for writes.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Instance;

public sealed class PatchModuleSettingsDto
{
    public OptionalUpdate<bool> EnableIslamicModule { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> EnableTechModule { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => EnableIslamicModule.HasValue || EnableTechModule.HasValue;
}

public sealed class PatchEventPolicyDto
{
    public OptionalUpdate<bool> AllowUserSubmittedEvents { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowOrganizationSubmittedEvents { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowGroupSubmittedEvents { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> EventCardClickOpensDetailPage { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantEventCardClickBehavior { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => AllowUserSubmittedEvents.HasValue || AllowOrganizationSubmittedEvents.HasValue
        || AllowGroupSubmittedEvents.HasValue || EventCardClickOpensDetailPage.HasValue || LockTenantEventCardClickBehavior.HasValue;
}

public sealed class PatchOrganizationPolicyDto
{
    public OptionalUpdate<bool> RequireOrganizationVerification { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowTenantToOmitVerification { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowOrganizationSelfRegistration { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowGroupSelfRegistration { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => RequireOrganizationVerification.HasValue || AllowTenantToOmitVerification.HasValue
        || AllowOrganizationSelfRegistration.HasValue || AllowGroupSelfRegistration.HasValue;
}

public sealed class PatchBrandingSettingsDto
{
    public OptionalUpdate<string?> DefaultBrandDisplayName { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> DefaultBrandLogoUrl { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> DefaultBrandFaviconUrl { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> DefaultBrandCustomCssUrl { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> LockTenantBrandDisplayName { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantBrandLogoUrl { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantBrandFaviconUrl { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantBrandCustomCssUrl { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => DefaultBrandDisplayName.HasValue || DefaultBrandLogoUrl.HasValue || DefaultBrandFaviconUrl.HasValue
        || DefaultBrandCustomCssUrl.HasValue || LockTenantBrandDisplayName.HasValue || LockTenantBrandLogoUrl.HasValue
        || LockTenantBrandFaviconUrl.HasValue || LockTenantBrandCustomCssUrl.HasValue;
}

public sealed class PatchDomainSettingsDto
{
    public OptionalUpdate<string?> InstanceBaseDomain { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> AdminHost { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> AllowTenantCustomDomains { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantSubdomain { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantCustomDomain { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => InstanceBaseDomain.HasValue || AdminHost.HasValue || AllowTenantCustomDomains.HasValue
        || LockTenantSubdomain.HasValue || LockTenantCustomDomain.HasValue;
}

public sealed class PatchTenantDelegationSettingsDto
{
    public OptionalUpdate<bool> AllowTenantSelfServiceRegistration { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowTenantWhiteLabeling { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> DefaultPublicHomePage { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> LockTenantHomePagePreference { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantSmtp { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantStorage { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantAnalytics { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantAiAssistant { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => AllowTenantSelfServiceRegistration.HasValue || AllowTenantWhiteLabeling.HasValue
        || DefaultPublicHomePage.HasValue || LockTenantHomePagePreference.HasValue || LockTenantSmtp.HasValue
        || LockTenantStorage.HasValue || LockTenantAnalytics.HasValue || LockTenantAiAssistant.HasValue;
}

public sealed class PatchAdminPortalSettingsDto
{
    public OptionalUpdate<bool> Enabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> PublicUrl { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> AllowTenantAdminAccess { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => Enabled.HasValue || PublicUrl.HasValue || AllowTenantAdminAccess.HasValue;
}

public sealed class PatchAiAssistantGovernanceSettingsDto
{
    public OptionalUpdate<bool> Enabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<AiAssistantProviderConfigurationWriteDto> ProviderConfiguration { get; set; } = OptionalUpdate<AiAssistantProviderConfigurationWriteDto>.Unspecified();
    public OptionalUpdate<bool> AllowAnonymousAccess { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> ToolProposalsEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantAiAssistant { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => Enabled.HasValue || ProviderConfiguration.HasValue || AllowAnonymousAccess.HasValue
        || ToolProposalsEnabled.HasValue || LockTenantAiAssistant.HasValue;
}

public sealed class AiAssistantProviderConfigurationWriteDto
{
    public string Provider { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public IReadOnlyList<string> AllowedModelIds { get; set; } = [];
}

public sealed class PatchMcpGovernanceSettingsDto
{
    public OptionalUpdate<bool> Enabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> EnableLegacySse { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantMcp { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantMcpLegacySse { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => Enabled.HasValue || EnableLegacySse.HasValue || LockTenantMcp.HasValue || LockTenantMcpLegacySse.HasValue;
}

public sealed class PatchRenderPolicySettingsDto
{
    public OptionalUpdate<string?> RenderPolicyPreset { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> EnableAdvancedRenderPolicyOverrides { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> GlobalRenderMode { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> GlobalPrerenderEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> PublicSeoRenderMode { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> PublicSeoPrerenderEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> OperationalRenderMode { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> OperationalPrerenderEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> AdminRenderMode { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> AdminPrerenderEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<string?> OnboardingRenderMode { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<bool> OnboardingPrerenderEnabled { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> AllowTenantRenderPolicyOverride { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantPublicSeoRenderPolicy { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantOperationalRenderPolicy { get; set; } = OptionalUpdate<bool>.Unspecified();
    public OptionalUpdate<bool> LockTenantAdminRenderPolicy { get; set; } = OptionalUpdate<bool>.Unspecified();
    public bool HasChanges() => RenderPolicyPreset.HasValue || EnableAdvancedRenderPolicyOverrides.HasValue
        || GlobalRenderMode.HasValue || GlobalPrerenderEnabled.HasValue || PublicSeoRenderMode.HasValue
        || PublicSeoPrerenderEnabled.HasValue || OperationalRenderMode.HasValue || OperationalPrerenderEnabled.HasValue
        || AdminRenderMode.HasValue || AdminPrerenderEnabled.HasValue || OnboardingRenderMode.HasValue
        || OnboardingPrerenderEnabled.HasValue || AllowTenantRenderPolicyOverride.HasValue
        || LockTenantPublicSeoRenderPolicy.HasValue || LockTenantOperationalRenderPolicy.HasValue || LockTenantAdminRenderPolicy.HasValue;
}
