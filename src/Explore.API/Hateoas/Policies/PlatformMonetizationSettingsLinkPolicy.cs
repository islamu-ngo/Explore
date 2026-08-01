// ABOUTME: Defines permission-bound HAL candidates for the singleton instance platform monetization settings document.
// ABOUTME: Keeps view and edit affordances aligned to the same instance-setting key and server authorization actions.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.PlatformMonetization;
using Explore.Application.Features.PlatformMonetization.Requests.Queries;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class PlatformMonetizationSettingsLinkPolicy : ILinkPolicy<PlatformMonetizationSettingsDto>
{
    public IEnumerable<LinkDefinition> GetLinks(PlatformMonetizationSettingsDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(RouteNames.GetInstancePlatformMonetizationSettings)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                GetPlatformMonetizationSettingsQuery.SettingKey,
                Attributes());

        yield return new LinkDefinition(
                LinkRelations.Edit,
                RouteNames.UpdateInstancePlatformMonetizationSettings,
                null,
                HttpMethods.Put,
                RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                ResourceKinds.InstanceSetting,
                GetPlatformMonetizationSettingsQuery.SettingKey,
                Attributes());
    }

    private static IReadOnlyDictionary<string, object> Attributes() => new Dictionary<string, object>
    {
        ["settingKey"] = GetPlatformMonetizationSettingsQuery.SettingKey
    };
}

public sealed class PlatformMonetizationSettingsCollectionLinkPolicy : ICollectionLinkPolicy<PlatformMonetizationSettingsDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(PlatformMonetizationSettingsDto dto, ClaimsPrincipal? user)
    {
        yield break;
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield break;
    }
}
