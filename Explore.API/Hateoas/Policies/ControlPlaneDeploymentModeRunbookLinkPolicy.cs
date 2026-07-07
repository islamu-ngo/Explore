// ABOUTME: HAL link policy for the Control Plane deployment-mode migration runbook.
// ABOUTME: Emits transition affordances only when server-calculated preconditions allow them.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;

namespace Explore.API.Hateoas.Policies;

public sealed class ControlPlaneDeploymentModeRunbookLinkPolicy : ILinkPolicy<ControlPlaneDeploymentModeRunbookDto>
{
    public IEnumerable<LinkDefinition> GetLinks(ControlPlaneDeploymentModeRunbookDto resource, ClaimsPrincipal? user)
    {
        yield return ViewLink(
            LinkRelations.Self,
            RouteNames.GetControlPlaneDeploymentModeRunbook,
            "Deployment mode runbook");

        yield return ViewLink(
            "operations",
            RouteNames.GetControlPlaneOperations,
            "Control Plane operations");

        foreach (var option in resource.TargetOptions.Where(option => option.Allowed))
        {
            if (!Enum.TryParse<DeploymentMode>(option.TargetMode, ignoreCase: false, out var targetMode))
            {
                continue;
            }

            yield return TransitionLink(targetMode, option.Label);
        }
    }

    private static LinkDefinition ViewLink(string rel, string routeName, string title) =>
        new LinkDefinition(rel, routeName, null, "GET", title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                GetControlPlaneDeploymentModeRunbookQuery.SettingKey,
                InstanceSettingAttributes(GetControlPlaneDeploymentModeRunbookQuery.SettingKey));

    private static LinkDefinition TransitionLink(DeploymentMode targetMode, string title)
    {
        var rel = targetMode == DeploymentMode.MultiTenant
            ? LinkRelations.TransitionToMultiTenant
            : LinkRelations.TransitionToSingleTenant;

        return new LinkDefinition(
                rel,
                RouteNames.TransitionControlPlaneDeploymentMode,
                null,
                "POST",
                title,
                RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                ResourceKinds.InstanceSetting,
                TransitionControlPlaneDeploymentModeCommand.SettingKey,
                InstanceSettingAttributes(TransitionControlPlaneDeploymentModeCommand.SettingKey, targetMode));
    }

    private static IReadOnlyDictionary<string, object> InstanceSettingAttributes(
        string settingKey,
        DeploymentMode? targetMode = null)
    {
        var attributes = new Dictionary<string, object>
        {
            ["settingKey"] = settingKey
        };

        if (targetMode is not null)
        {
            attributes["targetMode"] = targetMode.Value.ToString();
        }

        return attributes;
    }
}

public sealed class ControlPlaneDeploymentModeRunbookCollectionLinkPolicy
    : ICollectionLinkPolicy<ControlPlaneDeploymentModeRunbookDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(ControlPlaneDeploymentModeRunbookDto item, ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
