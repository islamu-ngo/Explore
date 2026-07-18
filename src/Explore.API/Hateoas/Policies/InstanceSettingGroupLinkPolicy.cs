// ABOUTME: Authorization-aware HAL links for the ATProto instance-governance setting group.
// ABOUTME: Advertises allowlisted update and lock transitions only when server metadata permits them.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Settings;
using Explore.Application.Hateoas;
using Explore.Domain.Settings.Definitions;

public sealed class AtprotoInstanceSettingGroupLinkPolicy : ILinkPolicy<SettingGroupResponseDto>
{
    public IEnumerable<LinkDefinition> GetLinks(SettingGroupResponseDto dto, ClaimsPrincipal? user)
    {
        yield return Link(
            LinkRelations.Self,
            RouteNames.GetInstanceAtprotoFederationSettings,
            "GET",
            null)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                AtprotoFederationSettingDefinitions.Category,
                Attributes(AtprotoFederationSettingDefinitions.Category));

        foreach (var setting in dto.Settings.Where(setting =>
                     setting.CanEdit && AtprotoFederationSettingDefinitions.IsAdministratorKey(setting.Key)))
        {
            yield return Link(
                $"update-{setting.Key}",
                RouteNames.UpdateInstanceAtprotoFederationSetting,
                "PUT",
                new { key = setting.Key })
                .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                    ResourceKinds.InstanceSetting,
                    setting.Key,
                    Attributes(setting.Key));

            if (!setting.IsLockable)
            {
                continue;
            }

            yield return setting.IsLocked
                ? Link(
                    $"unlock-{setting.Key}",
                    RouteNames.UnlockInstanceAtprotoFederationSetting,
                    "DELETE",
                    new { key = setting.Key })
                    .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                        ResourceKinds.InstanceSetting,
                        setting.Key,
                        Attributes(setting.Key))
                : Link(
                    $"lock-{setting.Key}",
                    RouteNames.LockInstanceAtprotoFederationSetting,
                    "POST",
                    new { key = setting.Key })
                    .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                        ResourceKinds.InstanceSetting,
                        setting.Key,
                        Attributes(setting.Key));
        }
    }

    private static LinkDefinition Link(
        string relation,
        string routeName,
        string method,
        object? routeValues) =>
        new LinkDefinition(
            relation,
            routeName,
            routeValues,
            method,
            RequiresAuth: true);

    private static IReadOnlyDictionary<string, object> Attributes(string settingKey) =>
        new Dictionary<string, object> { ["settingKey"] = settingKey };
}

public sealed class AtprotoInstanceSettingGroupCollectionLinkPolicy : ICollectionLinkPolicy<SettingGroupResponseDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(SettingGroupResponseDto dto, ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
