// ABOUTME: HAL policy for tenant moderation-reporting dashboard links.
// ABOUTME: Links dashboards to routing-state actions while keeping all affordances server-authorized.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Hateoas;

public sealed class TenantModerationReportingDashboardLinkPolicy : ILinkPolicy<TenantModerationReportingDashboardDto>
{
    public IEnumerable<LinkDefinition> GetLinks(TenantModerationReportingDashboardDto dto, ClaimsPrincipal? user)
    {
        _ = user;

        yield return LinkDefinition.Self(RouteNames.GetTenantModerationReportingDashboard)
            .RequirePermission(AuthorizationActions.TenantSettings.View,
                ResourceKinds.TenantSetting,
                TenantDashboardResourceId(dto),
                TenantDashboardAttributes(dto),
                TenantDashboardScope(dto));

        yield return new LinkDefinition(
                "routing-state",
                RouteNames.GetModerationReportingRoutingState,
                null,
                "GET",
                "Moderation reporting routing state",
                RequiresAuth: true)
            .RequirePermission(AuthorizationActions.TenantSettings.View,
                ResourceKinds.TenantSetting,
                TenantDashboardResourceId(dto),
                TenantDashboardAttributes(dto),
                TenantDashboardScope(dto));
    }

    private static string TenantDashboardResourceId(TenantModerationReportingDashboardDto dto)
    {
        return $"{dto.TenantId}:moderation-reporting";
    }

    private static IReadOnlyDictionary<string, object> TenantDashboardAttributes(TenantModerationReportingDashboardDto dto)
    {
        return new Dictionary<string, object>
        {
            ["tenantId"] = dto.TenantId.ToString(),
            ["settingKey"] = "moderation-reporting"
        };
    }

    private static AuthorizationScope TenantDashboardScope(TenantModerationReportingDashboardDto dto)
    {
        return new AuthorizationScope(TenantId: dto.TenantId.ToString());
    }
}

public sealed class TenantModerationReportingDashboardCollectionLinkPolicy
    : ICollectionLinkPolicy<TenantModerationReportingDashboardDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(TenantModerationReportingDashboardDto dto, ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
