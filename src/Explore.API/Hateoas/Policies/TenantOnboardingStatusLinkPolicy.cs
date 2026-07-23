// ABOUTME: HAL policies for tenant onboarding completion and post-launch management affordances.
// ABOUTME: Encodes tenant and platform authority as permission-checked links for Blazor consumers.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;

public sealed class TenantOnboardingStatusLinkPolicy : ILinkPolicy<TenantOnboardingStatusDto>
{
    private const string TenantOnboardingSettingKey = "onboarding";

    public IEnumerable<LinkDefinition> GetLinks(TenantOnboardingStatusDto dto, ClaimsPrincipal? user)
    {
        _ = user;

        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantOnboardingStatus,
            Method: HttpMethods.Get,
            Title: "Tenant onboarding status",
            RequiresAuth: true);

        if (dto.TenantId == Guid.Empty || !dto.IsAuthenticated)
        {
            yield break;
        }

        if (dto.IsCurrentUserTenantAdministrator)
        {
            yield return new LinkDefinition(
                "manage-tenant-settings",
                RouteNames.GetTenantOnboardingPolicySettings,
                Method: HttpMethods.Get,
                Title: "Manage tenant settings",
                RequiresAuth: true)
                 .RequirePermission(AuthorizationActions.TenantSettings.Update,
                    ResourceKinds.TenantSetting,
                    $"{dto.TenantId}:{TenantOnboardingSettingKey}",
                    TenantAttributes(dto.TenantId),
                    new AuthorizationScope(TenantId: dto.TenantId.ToString()));

            if (!dto.IsCompleted)
            {
                yield return new LinkDefinition(
                    "complete",
                    RouteNames.CompleteTenantOnboarding,
                    Method: HttpMethods.Post,
                    Title: "Complete tenant onboarding",
                    RequiresAuth: true)
                    .RequirePermission(AuthorizationActions.TenantSettings.Update,
                        ResourceKinds.TenantSetting,
                        $"{dto.TenantId}:{TenantOnboardingSettingKey}",
                        TenantAttributes(dto.TenantId),
                        new AuthorizationScope(TenantId: dto.TenantId.ToString()));
            }
        }

        if (!dto.IsCurrentUserPlatformAdministrator)
        {
            yield break;
        }

        yield return new LinkDefinition(
            "manage-control-plane",
            RouteNames.GetControlPlaneTenantById,
            new { tenantId = dto.TenantId },
            HttpMethods.Get,
            "Manage tenant from the control plane",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                GetControlPlaneTenantListQuery.SettingKey,
                InstanceAttributes(dto.TenantId));

        if (!dto.IsCompleted && !dto.IsCurrentUserTenantAdministrator)
        {
            yield return new LinkDefinition(
                "complete",
                RouteNames.CompleteTenantOnboarding,
                Method: HttpMethods.Post,
                Title: "Complete tenant onboarding",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                    ResourceKinds.InstanceSetting,
                    GetControlPlaneTenantListQuery.SettingKey,
                    InstanceAttributes(dto.TenantId));
        }
    }

    private static Dictionary<string, object> TenantAttributes(Guid tenantId) =>
        new Dictionary<string, object>
        {
            ["tenantId"] = tenantId.ToString(),
            ["settingKey"] = TenantOnboardingSettingKey,
            ["isLockedByInstance"] = false
        };

    private static Dictionary<string, object> InstanceAttributes(Guid tenantId) =>
        new Dictionary<string, object>
        {
            ["settingKey"] = GetControlPlaneTenantListQuery.SettingKey,
            ["tenantId"] = tenantId.ToString()
        };
}

public sealed class TenantOnboardingStatusCollectionLinkPolicy
    : ICollectionLinkPolicy<TenantOnboardingStatusDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(TenantOnboardingStatusDto dto, ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
