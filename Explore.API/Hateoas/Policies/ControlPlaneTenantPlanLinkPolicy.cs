// ABOUTME: HAL link policies for control-plane tenant plan SaaS tier resources.
// ABOUTME: Emits instance-setting permission metadata for plan template affordances.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;

public sealed class ControlPlaneTenantPlanDetailLinkPolicy : ILinkPolicy<ControlPlaneTenantPlanDetailDto>
{
    public IEnumerable<LinkDefinition> GetLinks(ControlPlaneTenantPlanDetailDto dto, ClaimsPrincipal? user)
    {
        _ = user;

        yield return ViewLink(
            LinkRelations.Self,
            RouteNames.GetControlPlaneTenantPlanByKey,
            new { key = dto.Key },
            dto.DisplayName,
            dto.Key);

        yield return ViewLink(
            LinkRelations.Collection,
            RouteNames.GetControlPlaneTenantPlans,
            null,
            "Control-plane tenant plans",
            dto.Key);

        yield return UpdateLink(
            "create-version-draft",
            RouteNames.CreateControlPlaneTenantPlanVersionDraft,
            new { key = dto.Key },
            "Create plan version draft",
            CreateControlPlaneTenantPlanVersionDraftCommand.SettingKey,
            dto.Key);

        yield return ViewLink(
            "validate",
            RouteNames.ValidateControlPlaneTenantPlanDraft,
            null,
            "Validate tenant plan draft",
            dto.Key,
            ValidateControlPlaneTenantPlanDraftQuery.SettingKey,
            method: "POST");

        yield return ViewLink(
            "preview-diff",
            RouteNames.PreviewControlPlaneTenantPlanDiff,
            null,
            "Preview tenant plan diff",
            dto.Key,
            PreviewControlPlaneTenantPlanDiffQuery.SettingKey,
            method: "POST");

        foreach (var version in dto.Versions)
        {
            yield return UpdateLink(
                "update-version-draft",
                RouteNames.UpdateControlPlaneTenantPlanVersionDraft,
                new { versionId = version.Id },
                "Update plan version draft",
                UpdateControlPlaneTenantPlanVersionDraftCommand.SettingKey,
                dto.Key,
                versionId: version.Id);

            yield return UpdateLink(
                LinkRelations.Publish,
                RouteNames.PublishControlPlaneTenantPlanVersion,
                new { versionId = version.Id },
                "Publish plan version",
                PublishControlPlaneTenantPlanVersionCommand.SettingKey,
                dto.Key,
                versionId: version.Id);

            yield return UpdateLink(
                LinkRelations.Archive,
                RouteNames.ArchiveControlPlaneTenantPlanVersion,
                new { versionId = version.Id },
                "Archive plan version",
                ArchiveControlPlaneTenantPlanVersionCommand.SettingKey,
                dto.Key,
                versionId: version.Id);

            yield return UpdateLink(
                "clone",
                RouteNames.CloneControlPlaneTenantPlan,
                new { sourceVersionId = version.Id },
                "Clone plan version",
                CloneControlPlaneTenantPlanCommand.SettingKey,
                dto.Key,
                sourceVersionId: version.Id);
        }
    }

    private static LinkDefinition ViewLink(
        string rel,
        string routeName,
        object? routeValues,
        string title,
        string planKey,
        string settingKey = GetControlPlaneTenantPlanListQuery.SettingKey,
        string method = "GET") =>
        new LinkDefinition(rel, routeName, routeValues, method, title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                settingKey,
                InstanceSettingAttributes(settingKey, planKey));

    private static LinkDefinition UpdateLink(
        string rel,
        string routeName,
        object? routeValues,
        string title,
        string settingKey,
        string planKey,
        Guid? versionId = null,
        Guid? sourceVersionId = null) =>
        new LinkDefinition(rel, routeName, routeValues, routeName == RouteNames.UpdateControlPlaneTenantPlanVersionDraft ? "PUT" : "POST", title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                ResourceKinds.InstanceSetting,
                settingKey,
                InstanceSettingAttributes(settingKey, planKey, versionId, sourceVersionId));

    private static IReadOnlyDictionary<string, object> InstanceSettingAttributes(
        string settingKey,
        string? planKey = null,
        Guid? versionId = null,
        Guid? sourceVersionId = null)
    {
        var attributes = new Dictionary<string, object>
        {
            ["settingKey"] = settingKey
        };

        if (!string.IsNullOrWhiteSpace(planKey))
        {
            attributes["planKey"] = planKey;
        }

        if (versionId.HasValue)
        {
            attributes["versionId"] = versionId.Value;
        }

        if (sourceVersionId.HasValue)
        {
            attributes["sourceVersionId"] = sourceVersionId.Value;
        }

        return attributes;
    }
}

public sealed class ControlPlaneTenantPlanCollectionLinkPolicy : ICollectionLinkPolicy<ControlPlaneTenantPlanListItemDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(ControlPlaneTenantPlanListItemDto dto, ClaimsPrincipal? user)
    {
        _ = user;

        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetControlPlaneTenantPlanByKey,
            new { key = dto.Key },
            "GET",
            dto.DisplayName,
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                GetControlPlaneTenantPlanListQuery.SettingKey,
                new Dictionary<string, object>
                {
                    ["settingKey"] = GetControlPlaneTenantPlanListQuery.SettingKey,
                    ["planKey"] = dto.Key
                });
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        _ = user;

        yield return new LinkDefinition(
            LinkRelations.Create,
            RouteNames.CreateControlPlaneTenantPlanDraft,
            null,
            "POST",
            "Create tenant plan draft",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                ResourceKinds.InstanceSetting,
                CreateControlPlaneTenantPlanDraftCommand.SettingKey,
                new Dictionary<string, object>
                {
                    ["settingKey"] = CreateControlPlaneTenantPlanDraftCommand.SettingKey
                });

        yield return new LinkDefinition(
            "validate",
            RouteNames.ValidateControlPlaneTenantPlanDraft,
            null,
            "POST",
            "Validate tenant plan draft",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                ValidateControlPlaneTenantPlanDraftQuery.SettingKey,
                new Dictionary<string, object>
                {
                    ["settingKey"] = ValidateControlPlaneTenantPlanDraftQuery.SettingKey
                });

        yield return new LinkDefinition(
            "preview-diff",
            RouteNames.PreviewControlPlaneTenantPlanDiff,
            null,
            "POST",
            "Preview tenant plan diff",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                PreviewControlPlaneTenantPlanDiffQuery.SettingKey,
                new Dictionary<string, object>
                {
                    ["settingKey"] = PreviewControlPlaneTenantPlanDiffQuery.SettingKey
                });
    }
}
