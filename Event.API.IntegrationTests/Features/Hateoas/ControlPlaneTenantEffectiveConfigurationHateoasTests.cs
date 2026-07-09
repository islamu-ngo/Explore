// ABOUTME: Link-policy contract tests for tenant effective-configuration HAL affordances.
// ABOUTME: Protects tenant plan assignment actions from drifting away from server authorization metadata.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.ControlPlane.Requests.Queries;
using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Routing;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class ControlPlaneTenantEffectiveConfigurationHateoasTests
{
    [Test]
    public async Task DetailLinks_ExposeReadAndAssignmentAuthorizationMetadata()
    {
        var policy = new ControlPlaneTenantEffectiveConfigurationLinkPolicy();
        var configuration = CreateConfiguration();

        var links = policy.GetLinks(configuration, user: null).ToArray();

        var self = links.Single(link => link.Rel == LinkRelations.Self);
        await Assert.That(self.RouteName).IsEqualTo(RouteNames.GetControlPlaneTenantEffectiveConfiguration);
        await Assert.That(self.Method).IsEqualTo("GET");
        await Assert.That(RouteValues(self)["tenantId"]).IsEqualTo(configuration.TenantId);
        await Assert.That(self.RequiresAuth).IsTrue();
        await Assert.That(self.PermissionResourceKind).IsEqualTo(ResourceKinds.InstanceSetting);
        await Assert.That(self.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(self.PermissionResourceId).IsEqualTo(GetControlPlaneTenantEffectiveConfigurationQuery.SettingKey);
        await Assert.That(self.PermissionResourceAttributes?["settingKey"]).IsEqualTo(GetControlPlaneTenantEffectiveConfigurationQuery.SettingKey);
        await Assert.That(self.PermissionResourceAttributes?["tenantId"]).IsEqualTo(configuration.TenantId);

        var assignment = links.Single(link => link.Rel == "plan-assignment");
        await Assert.That(assignment.RouteName).IsEqualTo(RouteNames.GetControlPlaneTenantPlanAssignment);
        await Assert.That(assignment.Method).IsEqualTo("GET");
        await Assert.That(assignment.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.View);
        await Assert.That(assignment.PermissionResourceId).IsEqualTo(GetControlPlaneTenantPlanAssignmentQuery.SettingKey);
        await Assert.That(RouteValues(assignment)["tenantId"]).IsEqualTo(configuration.TenantId);
    }

    [Test]
    public async Task DetailLinks_ExposePlanAssignmentMutationAuthorizationMetadata()
    {
        var policy = new ControlPlaneTenantEffectiveConfigurationLinkPolicy();
        var configuration = CreateConfiguration();
        var assignmentId = configuration.PlanAssignment!.Id;

        var links = policy.GetLinks(configuration, user: null).ToArray();

        var switchPlan = links.Single(link => link.Rel == "switch-plan");
        await Assert.That(switchPlan.RouteName).IsEqualTo(RouteNames.SwitchControlPlaneTenantPlanAssignment);
        await Assert.That(switchPlan.Method).IsEqualTo("POST");
        await Assert.That(RouteValues(switchPlan)["tenantId"]).IsEqualTo(configuration.TenantId);
        await Assert.That(switchPlan.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.Update);
        await Assert.That(switchPlan.PermissionResourceId).IsEqualTo(SwitchControlPlaneTenantPlanAssignmentCommand.SettingKey);
        await Assert.That(switchPlan.PermissionResourceAttributes?["tenantId"]).IsEqualTo(configuration.TenantId);

        var apply = links.Single(link => link.Rel == "apply");
        await Assert.That(apply.RouteName).IsEqualTo(RouteNames.ApplyControlPlaneTenantPlanAssignment);
        await Assert.That(apply.Method).IsEqualTo("POST");
        await Assert.That(RouteValues(apply)["tenantId"]).IsEqualTo(configuration.TenantId);
        await Assert.That(RouteValues(apply)["assignmentId"]).IsEqualTo(assignmentId);
        await Assert.That(apply.PermissionResourceId).IsEqualTo(ApplyControlPlaneTenantPlanAssignmentCommand.SettingKey);
        await Assert.That(apply.PermissionResourceAttributes?["assignmentId"]).IsEqualTo(assignmentId);

        var rollback = links.Single(link => link.Rel == "rollback");
        await Assert.That(rollback.RouteName).IsEqualTo(RouteNames.RollbackControlPlaneTenantPlanAssignment);
        await Assert.That(rollback.Method).IsEqualTo("POST");
        await Assert.That(RouteValues(rollback)["tenantId"]).IsEqualTo(configuration.TenantId);
        await Assert.That(RouteValues(rollback)["assignmentId"]).IsEqualTo(assignmentId);
        await Assert.That(rollback.PermissionResourceId).IsEqualTo(RollbackControlPlaneTenantPlanAssignmentCommand.SettingKey);
        await Assert.That(rollback.PermissionResourceAttributes?["assignmentId"]).IsEqualTo(assignmentId);
    }

    [Test]
    public async Task DetailLinks_OmitApplyAndRollback_WhenNoAssignmentExists()
    {
        var policy = new ControlPlaneTenantEffectiveConfigurationLinkPolicy();
        var configuration = CreateConfiguration();
        configuration.PlanAssignment = null;

        var links = policy.GetLinks(configuration, user: null).ToArray();

        await Assert.That(links.Any(link => link.Rel == "switch-plan")).IsTrue();
        await Assert.That(links.Any(link => link.Rel == "apply")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "rollback")).IsFalse();
    }

    [Test]
    public async Task DetailLinks_ExposeSettingOverrideLockAndUnlockAuthorizationMetadata()
    {
        var policy = new ControlPlaneTenantEffectiveConfigurationLinkPolicy();
        var configuration = CreateConfiguration();
        configuration.Settings =
        [
            new ControlPlaneTenantEffectiveSettingDto
            {
                Key = "branding.display_name",
                Category = "branding",
                Value = "Demo Tenant",
                ValueSource = "TenantOverride",
                IsLocked = false,
                IsSensitive = false
            },
            new ControlPlaneTenantEffectiveSettingDto
            {
                Key = "storage.max_upload_bytes",
                Category = "storage",
                Value = "104857600",
                ValueSource = "SystemLocked",
                IsLocked = true,
                IsSensitive = false
            },
            new ControlPlaneTenantEffectiveSettingDto
            {
                Key = "security.oidc_client_secret",
                Category = "security",
                Value = string.Empty,
                ValueSource = "TenantOverride",
                IsLocked = false,
                IsSensitive = true
            }
        ];

        var links = policy.GetLinks(configuration, user: null).ToArray();

        var overrideBranding = links.Single(link => link.Rel == "override"
            && RouteValues(link)["key"] as string == "branding.display_name");
        await Assert.That(overrideBranding.RouteName).IsEqualTo(RouteNames.SetControlPlaneTenantSetting);
        await Assert.That(overrideBranding.Method).IsEqualTo("PUT");
        await Assert.That(RouteValues(overrideBranding)["tenantId"]).IsEqualTo(configuration.TenantId);
        await Assert.That(overrideBranding.PermissionAction).IsEqualTo(AuthorizationActions.InstanceSettings.Update);
        await Assert.That(overrideBranding.PermissionResourceId).IsEqualTo(LockControlPlaneTenantSettingCommand.SettingKey);
        await Assert.That(overrideBranding.PermissionResourceAttributes?["targetKey"]).IsEqualTo("branding.display_name");

        var lockBranding = links.Single(link => link.Rel == "lock"
            && RouteValues(link)["key"] as string == "branding.display_name");
        await Assert.That(lockBranding.RouteName).IsEqualTo(RouteNames.LockControlPlaneTenantSetting);
        await Assert.That(lockBranding.Method).IsEqualTo("POST");
        await Assert.That(lockBranding.PermissionResourceId).IsEqualTo(LockControlPlaneTenantSettingCommand.SettingKey);

        var unlockStorage = links.Single(link => link.Rel == "unlock"
            && RouteValues(link)["key"] as string == "storage.max_upload_bytes");
        await Assert.That(unlockStorage.RouteName).IsEqualTo(RouteNames.UnlockControlPlaneTenantSetting);
        await Assert.That(unlockStorage.Method).IsEqualTo("DELETE");
        await Assert.That(unlockStorage.PermissionResourceId).IsEqualTo(UnlockControlPlaneTenantSettingCommand.SettingKey);

        await Assert.That(links.Any(link => link.Rel == "override"
            && RouteValues(link)["key"] as string == "security.oidc_client_secret")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "lock"
            && RouteValues(link)["key"] as string == "security.oidc_client_secret")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "unlock"
            && RouteValues(link)["key"] as string == "security.oidc_client_secret")).IsFalse();
    }

    private static ControlPlaneTenantEffectiveConfigurationDto CreateConfiguration()
    {
        Guid tenantId = Guid.NewGuid();

        return new ControlPlaneTenantEffectiveConfigurationDto
        {
            TenantId = tenantId,
            PlanAssignment = new ControlPlaneTenantPlanAssignmentDto
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PlanId = Guid.NewGuid(),
                PlanKey = "community",
                PlanVersionId = Guid.NewGuid(),
                VersionNumber = 1,
                StatusCode = "ACTIVE",
                AssignedAt = DateTime.UtcNow
            }
        };
    }

    private static RouteValueDictionary RouteValues(LinkDefinition link) => new(link.RouteValues);
}
