// ABOUTME: Defines the canonical named-lock set for instance branding governance.
// ABOUTME: Keeps ordinary branding commands and configuration-manifest preflight on one authority fence.

namespace Explore.Application.Settings;

using Explore.Domain.Constants;

public static class TenantBrandingGovernanceMutationLockKeys
{
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(
    [
        GovernanceSettingKeys.Deployment.Mode,
        GovernanceSettingKeys.Tenants.WhiteLabelingEnabled,
        GovernanceSettingKeys.Branding.DisplayName,
        GovernanceSettingKeys.Branding.LogoUrl,
        GovernanceSettingKeys.Branding.FaviconUrl,
        GovernanceSettingKeys.Branding.CustomCssUrl
    ]);
}
