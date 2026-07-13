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
using Explore.Domain.Settings;

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

        if (dto.RollbackAssignment is not null)
        {
            yield return UpdateLink(
                "rollback",
                RouteNames.RollbackControlPlaneTenantPlanAssignment,
                new { tenantId = dto.TenantId, assignmentId = dto.RollbackAssignment.Id },
                "POST",
                "Rollback tenant plan assignment",
                RollbackControlPlaneTenantPlanAssignmentCommand.SettingKey,
                dto.TenantId,
                dto.RollbackAssignment.Id);
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

internal static class ControlPlaneTenantEffectiveSettingLinks
{
    public static IEnumerable<LinkDefinition> GetLinks(
        Guid tenantId,
        ControlPlaneTenantEffectiveSettingDto setting)
    {
        SettingDefinition? definition = SettingRegistry.Get(setting.Key);
        if (definition is null
            || SettingScope.Tenant < definition.MinScope
            || SettingScope.Tenant > definition.MaxScope
            || definition.IsSensitive
            || setting.IsSensitive
            || string.Equals(setting.ValueSource, "SystemLocked", StringComparison.Ordinal)
            || string.Equals(setting.LockSource, "SystemLocked", StringComparison.Ordinal))
        {
            yield break;
        }

        yield return UpdateLink(
            "override",
            RouteNames.SetControlPlaneTenantSetting,
            "PUT",
            $"Override setting '{setting.Key}'",
            SetControlPlaneTenantSettingCommand.SettingKey,
            tenantId,
            setting.Key);

        if (definition.IsLockable
            && string.Equals(setting.ValueSource, "TenantLocked", StringComparison.Ordinal))
        {
            yield return UpdateLink(
                "unlock",
                RouteNames.UnlockControlPlaneTenantSetting,
                "DELETE",
                $"Unlock setting '{setting.Key}'",
                UnlockControlPlaneTenantSettingCommand.SettingKey,
                tenantId,
                setting.Key);
        }
        else if (definition.IsLockable
            && string.Equals(setting.ValueSource, "TenantOverride", StringComparison.Ordinal))
        {
            yield return UpdateLink(
                "lock",
                RouteNames.LockControlPlaneTenantSetting,
                "POST",
                $"Lock setting '{setting.Key}'",
                LockControlPlaneTenantSettingCommand.SettingKey,
                tenantId,
                setting.Key);
        }
    }

    private static LinkDefinition UpdateLink(
        string relation,
        string routeName,
        string method,
        string title,
        string resourceId,
        Guid tenantId,
        string settingKey) =>
        new LinkDefinition(
            relation,
            routeName,
            new { tenantId, key = settingKey },
            method,
            title,
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.InstanceSettings.Update,
                ResourceKinds.InstanceSetting,
                resourceId,
                new Dictionary<string, object>
                {
                    ["settingKey"] = resourceId,
                    ["tenantId"] = tenantId.ToString(),
                    ["targetKey"] = settingKey
                });
}

public sealed class ControlPlaneTenantEffectiveConfigurationCollectionLinkPolicy
    : ICollectionLinkPolicy<ControlPlaneTenantEffectiveConfigurationDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(ControlPlaneTenantEffectiveConfigurationDto dto, ClaimsPrincipal? user) => [];

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user) => [];
}
