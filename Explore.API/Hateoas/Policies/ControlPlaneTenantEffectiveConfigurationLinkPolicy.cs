// ABOUTME: HAL link policy for a tenant's effective configuration read model.
// ABOUTME: Emits plan-assignment action metadata so clients keep using server HAL affordances.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;

public sealed class ControlPlaneTenantEffectiveConfigurationLinkPolicy
    : ILinkPolicy<ControlPlaneTenantEffectiveConfigurationDto>
{
    public IEnumerable<LinkDefinition> GetLinks(ControlPlaneTenantEffectiveConfigurationDto dto, ClaimsPrincipal? user)
    {
        _ = user;

        yield return ViewLink(
            LinkRelations.Self,
            RouteNames.GetControlPlaneTenantEffectiveConfiguration,
            new { tenantId = dto.TenantId },
            "Tenant effective configuration",
            GetControlPlaneTenantEffectiveConfigurationQuery.SettingKey,
            dto.TenantId);

        yield return ViewLink(
            "plan-assignment",
            RouteNames.GetControlPlaneTenantPlanAssignment,
            new { tenantId = dto.TenantId },
            "Tenant plan assignment",
            GetControlPlaneTenantPlanAssignmentQuery.SettingKey,
            dto.TenantId);

        yield return UpdateLink(
            "switch-plan",
            RouteNames.SwitchControlPlaneTenantPlanAssignment,
            new { tenantId = dto.TenantId },
            "POST",
            "Switch tenant plan assignment",
            SwitchControlPlaneTenantPlanAssignmentCommand.SettingKey,
            dto.TenantId);

        if (dto.PlanAssignment is null)
        {
            yield break;
        }

        yield return UpdateLink(
            "apply",
            RouteNames.ApplyControlPlaneTenantPlanAssignment,
            new { tenantId = dto.TenantId, assignmentId = dto.PlanAssignment.Id },
            "POST",
            "Apply tenant plan assignment",
            ApplyControlPlaneTenantPlanAssignmentCommand.SettingKey,
            dto.TenantId,
            dto.PlanAssignment.Id);

        yield return UpdateLink(
            "rollback",
            RouteNames.RollbackControlPlaneTenantPlanAssignment,
            new { tenantId = dto.TenantId, assignmentId = dto.PlanAssignment.Id },
            "POST",
            "Rollback tenant plan assignment",
            RollbackControlPlaneTenantPlanAssignmentCommand.SettingKey,
            dto.TenantId,
            dto.PlanAssignment.Id);

        foreach (var setting in dto.Settings)
        {
            if (setting.IsSensitive)
            {
                continue;
            }

            yield return UpdateLink(
                "override",
                RouteNames.SetControlPlaneTenantSetting,
                new { tenantId = dto.TenantId, key = setting.Key },
                "PUT",
                $"Override setting '{setting.Key}'",
                LockControlPlaneTenantSettingCommand.SettingKey,
                dto.TenantId,
                null,
                setting.Key);

            if (setting.IsLocked)
            {
                yield return UpdateLink(
                    "unlock",
                    RouteNames.UnlockControlPlaneTenantSetting,
                    new { tenantId = dto.TenantId, key = setting.Key },
                    "DELETE",
                    $"Unlock setting '{setting.Key}'",
                    UnlockControlPlaneTenantSettingCommand.SettingKey,
                    dto.TenantId,
                    null,
                    setting.Key);
            }
            else
            {
                yield return UpdateLink(
                    "lock",
                    RouteNames.LockControlPlaneTenantSetting,
                    new { tenantId = dto.TenantId, key = setting.Key },
                    "POST",
                    $"Lock setting '{setting.Key}'",
                    LockControlPlaneTenantSettingCommand.SettingKey,
                    dto.TenantId,
                    null,
                    setting.Key);
            }
        }
    }

    private static LinkDefinition ViewLink(
        string rel,
        string routeName,
        object routeValues,
        string title,
        string settingKey,
        Guid tenantId) =>
        new LinkDefinition(rel, routeName, routeValues, "GET", title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.View,
                ResourceKinds.InstanceSetting,
                settingKey,
                InstanceSettingAttributes(settingKey, tenantId));

    private static LinkDefinition UpdateLink(
        string rel,
        string routeName,
        object routeValues,
        string method,
        string title,
        string settingKey,
        Guid tenantId,
        Guid? assignmentId = null,
        string? settingTargetKey = null) =>
        new LinkDefinition(rel, routeName, routeValues, method, title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                ResourceKinds.InstanceSetting,
                settingKey,
                InstanceSettingAttributes(settingKey, tenantId, assignmentId, settingTargetKey));

    private static IReadOnlyDictionary<string, object> InstanceSettingAttributes(
        string settingKey,
        Guid tenantId,
        Guid? assignmentId = null,
        string? targetKey = null)
    {
        var attributes = new Dictionary<string, object>
        {
            ["settingKey"] = settingKey,
            ["tenantId"] = tenantId
        };

        if (assignmentId.HasValue)
        {
            attributes["assignmentId"] = assignmentId.Value;
        }

        if (!string.IsNullOrEmpty(targetKey))
        {
            attributes["targetKey"] = targetKey;
        }

        return attributes;
    }
}

public sealed class ControlPlaneTenantEffectiveConfigurationCollectionLinkPolicy
    : ICollectionLinkPolicy<ControlPlaneTenantEffectiveConfigurationDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(ControlPlaneTenantEffectiveConfigurationDto dto, ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
