// ABOUTME: bUnit coverage for the shared control-plane plan detail and tenant configuration pages.
// ABOUTME: Proves HAL-gated version lifecycle actions and setting override/lock/unlock flows work correctly.

using System.Security.Claims;
using Event.ControlPlane.Client.Contracts;
using Event.ControlPlane.Client.Extensions;
using Event.ControlPlane.Client.Pages.Plans;
using Event.ControlPlane.Client.Pages.TenantConfiguration;
using Event.ControlPlane.Client.Services;
using NSubstitute;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class ControlPlanePlanDetailAndTenantConfigPageTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public ControlPlanePlanDetailAndTenantConfigPageTests()
    {
        _ctx.Services.AddEventControlPlaneClient();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task PlanDetailPage_WhenNoHostApiAdapterRegistered_ShowsFailClosedState()
    {
        var cut = _ctx.Render<ControlPlanePlanDetailPage>(parameters => parameters
            .Add(p => p.Key, "enterprise"));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Control-plane API unavailable", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected plan detail to render its fail-closed state.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("enterprise");
        await Assert.That(cut.Markup).Contains("The control-plane API adapter is not configured for this host.");
    }

    [Test]
    public async Task PlanDetailPage_RendersVersionsAndHalGatedActions()
    {
        var planService = Substitute.For<IControlPlanePlanService>();
        planService.GetPlanAsync("enterprise", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantPlanDetail(
                Guid.NewGuid(),
                "enterprise",
                "Enterprise",
                "Premium tier plan.",
                [
                    new ControlPlaneTenantPlanVersion(
                        Guid.NewGuid(),
                        1,
                        2,
                        "Published",
                        99.00m,
                        "EUR",
                        "monthly",
                        true,
                        [
                            new ControlPlaneTenantPlanSetting("ai.max_daily_messages", "1000", false),
                            new ControlPlaneTenantPlanSetting("storage.retention_days", "30", true)
                        ],
                        [new ControlPlaneTenantPlanQuota("storage.bytes", 10737418240L)]),
                    new ControlPlaneTenantPlanVersion(
                        Guid.NewGuid(),
                        2,
                        1,
                        "Draft",
                        149.00m,
                        "EUR",
                        "monthly",
                        false,
                        [],
                        [])
                ],
                Links(
                    ControlPlaneLinkRelations.Self,
                    ControlPlaneLinkRelations.CreateVersionDraft,
                    ControlPlaneLinkRelations.UpdateVersionDraft,
                    ControlPlaneLinkRelations.Publish,
                    ControlPlaneLinkRelations.Clone))));

        _ctx.Services.AddSingleton(planService);

        var cut = _ctx.Render<ControlPlanePlanDetailPage>(parameters => parameters
            .Add(p => p.Key, "enterprise"));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Version 1", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected plan versions to render.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Enterprise");
        await Assert.That(cut.Markup).Contains("New version draft");
        await Assert.That(cut.Markup).Contains("Publish");
        await Assert.That(cut.Markup).Contains("Clone");
        await Assert.That(cut.Markup).Contains("AI settings");
        await Assert.That(cut.Markup).Contains("Storage settings");
    }

    [Test]
    public async Task PlanDetailPage_PublishVersion_CallsServiceAndReloads()
    {
        var versionId = Guid.NewGuid();
        var planService = Substitute.For<IControlPlanePlanService>();
        planService.GetPlanAsync("enterprise", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantPlanDetail(
                Guid.NewGuid(),
                "enterprise",
                "Enterprise",
                null,
                [new ControlPlaneTenantPlanVersion(versionId, 1, 1, "Draft", 99m, "EUR", "monthly", true, [], [])],
                Links(ControlPlaneLinkRelations.Self, ControlPlaneLinkRelations.Publish))));
        planService.PublishPlanVersionAsync(versionId, ControlPlaneTenantPlanExistingAssignmentPolicy.LeaveExistingTenantsPinned, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Version published."));
        _ctx.Services.AddSingleton(planService);

        var cut = _ctx.Render<ControlPlanePlanDetailPage>(parameters => parameters
            .Add(p => p.Key, "enterprise"));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Publish", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected publish affordance to render.");
            }
        });

        cut.Find("button[aria-label='Publish version 1']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Version published.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected publish success message.");
            }
        });

        await planService.Received(1).PublishPlanVersionAsync(versionId, ControlPlaneTenantPlanExistingAssignmentPolicy.LeaveExistingTenantsPinned, Arg.Any<CancellationToken>());
        await planService.Received(2).GetPlanAsync("enterprise", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PlanDetailPage_EditDraftVersion_ValidatesSavesAndReloads()
    {
        var versionId = Guid.NewGuid();
        var planService = Substitute.For<IControlPlanePlanService>();
        planService.GetPlanAsync("enterprise", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantPlanDetail(
                Guid.NewGuid(),
                "enterprise",
                "Enterprise",
                null,
                [
                    new ControlPlaneTenantPlanVersion(
                        versionId,
                        2,
                        1,
                        "Draft",
                        149m,
                        "EUR",
                        "monthly",
                        false,
                        [new ControlPlaneTenantPlanSetting("ai.max_daily_messages", "1000", false)],
                        [new ControlPlaneTenantPlanQuota("storage.bytes", 10737418240L)])
                ],
                Links(ControlPlaneLinkRelations.Self, ControlPlaneLinkRelations.UpdateVersionDraft))));
        planService.ValidatePlanDraftAsync(Arg.Any<ControlPlaneTenantPlanDraft>(), Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantPlanValidationResult([])));
        planService.UpdatePlanVersionDraftAsync(versionId, Arg.Any<ControlPlaneTenantPlanDraft>(), Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Draft updated."));
        _ctx.Services.AddSingleton(planService);

        var cut = _ctx.Render<ControlPlanePlanDetailPage>(parameters => parameters
            .Add(p => p.Key, "enterprise"));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Edit draft", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected draft edit affordance to render.");
            }
        });

        cut.Find("button[aria-label='Edit version 2 draft']").Click();
        cut.Find("input[aria-label='Plan name']").Change("Enterprise Plus");
        cut.Find("input[aria-label='Price amount']").Change("199.95");
        cut.Find("input[aria-label='Currency code']").Change("usd");
        cut.Find("input[aria-label='Billing period']").Change("yearly");
        cut.Find("textarea[aria-label='Setting overrides']").Change("# AI\nai.max_daily_messages|2000|true");
        cut.Find("textarea[aria-label='Quota limits']").Change("storage.bytes|21474836480");

        cut.Find("button[aria-label='Validate draft version']").Click();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Draft is valid.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected draft validation success message.");
            }
        });

        cut.Find("button[aria-label='Save draft version']").Click();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Draft updated.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected draft save success message.");
            }
        });

        await planService.Received(1).ValidatePlanDraftAsync(
            Arg.Is<ControlPlaneTenantPlanDraft>(draft => MatchesEditedDraft(draft)),
            Arg.Any<CancellationToken>());
        await planService.Received(1).UpdatePlanVersionDraftAsync(
            versionId,
            Arg.Is<ControlPlaneTenantPlanDraft>(draft => draft != null && draft.Name == "Enterprise Plus"),
            Arg.Any<CancellationToken>());
        await planService.Received(2).GetPlanAsync("enterprise", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantConfigPage_WhenNoHostApiAdapterRegistered_ShowsFailClosedState()
    {
        var tenantId = Guid.NewGuid();
        var cut = _ctx.Render<ControlPlaneTenantConfigurationPage>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Control-plane API unavailable", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant config to render its fail-closed state.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Tenant configuration");
    }

    [Test]
    public async Task TenantConfigPage_RendersSettingsAndHalGatedActions()
    {
        var tenantId = Guid.NewGuid();
        var configService = Substitute.For<IControlPlaneTenantConfigurationService>();
        configService.GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantEffectiveConfiguration(
                tenantId,
                null,
                [
                    new ControlPlaneTenantEffectiveSetting(
                        "ai.max_daily_messages",
                        "AI",
                        "500",
                        2,
                        "Integer",
                        "Integer",
                        "PlanDefault",
                        false,
                        null,
                        "Maximum AI messages per day.",
                        false,
                        []),
                    new ControlPlaneTenantEffectiveSetting(
                        "secrets.smtp_password",
                        "Email",
                        "redacted",
                        1,
                        "String",
                        "String",
                        "InstanceDefault",
                        true,
                        "Instance",
                        "SMTP password.",
                        true,
                        [])
                ],
                [],
                Links(ControlPlaneLinkRelations.Override, ControlPlaneLinkRelations.Lock, ControlPlaneLinkRelations.Unlock))));
        _ctx.Services.AddSingleton(configService);

        var cut = _ctx.Render<ControlPlaneTenantConfigurationPage>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("ai.max_daily_messages", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected settings to render.");
            }
        });

        await Assert.That(cut.Markup).Contains("Override");
        await Assert.That(cut.Markup).Contains("Lock");
        await Assert.That(cut.Markup).Contains("••••••••");
    }

    [Test]
    public async Task TenantConfigPage_LockSetting_CallsServiceAndReloads()
    {
        var tenantId = Guid.NewGuid();
        var configService = Substitute.For<IControlPlaneTenantConfigurationService>();
        configService.GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantEffectiveConfiguration(
                tenantId,
                null,
                [new ControlPlaneTenantEffectiveSetting("ai.max_daily_messages", "AI", "500", 2, "Integer", "Integer", "PlanDefault", false, null, null, false, [])],
                [],
                Links(ControlPlaneLinkRelations.Lock))));
        configService.LockSettingAsync(tenantId, "ai.max_daily_messages", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Setting locked."));
        _ctx.Services.AddSingleton(configService);

        var cut = _ctx.Render<ControlPlaneTenantConfigurationPage>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Lock", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected lock affordance to render.");
            }
        });

        cut.Find("button[aria-label='Lock ai.max_daily_messages']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Setting locked.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected lock success message.");
            }
        });

        await configService.Received(1).LockSettingAsync(tenantId, "ai.max_daily_messages", Arg.Any<CancellationToken>());
        await configService.Received(2).GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantConfigPage_OverrideSetting_CallsServiceAndReloads()
    {
        var tenantId = Guid.NewGuid();
        var configService = Substitute.For<IControlPlaneTenantConfigurationService>();
        configService.GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantEffectiveConfiguration(
                tenantId,
                null,
                [new ControlPlaneTenantEffectiveSetting("ai.max_daily_messages", "AI", "500", 2, "Integer", "Integer", "PlanDefault", false, null, null, false, [])],
                [],
                Links(ControlPlaneLinkRelations.Override))));
        configService.SetSettingAsync(tenantId, "ai.max_daily_messages", "1000", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Setting overridden."));
        _ctx.Services.AddSingleton(configService);

        var cut = _ctx.Render<ControlPlaneTenantConfigurationPage>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Override", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected override affordance to render.");
            }
        });

        cut.Find("button[aria-label='Override ai.max_daily_messages']").Click();
        cut.Find("input[aria-label='Edit value for ai.max_daily_messages']").Change("1000");
        cut.Find("button[aria-label='Save override for ai.max_daily_messages']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Setting overridden.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected override success message.");
            }
        });

        await configService.Received(1).SetSettingAsync(tenantId, "ai.max_daily_messages", "1000", Arg.Any<CancellationToken>());
        await configService.Received(2).GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantConfigPage_ApplyAssignment_RequiresTypedConfirmationAndReloads()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var configService = Substitute.For<IControlPlaneTenantConfigurationService>();
        var planService = Substitute.For<IControlPlanePlanService>();
        configService.GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantEffectiveConfiguration(
                tenantId,
                new ControlPlaneTenantPlanAssignment(
                    assignmentId,
                    tenantId,
                    Guid.NewGuid(),
                    "enterprise",
                    Guid.NewGuid(),
                    3,
                    1,
                    "PendingApply",
                    DateTimeOffset.UtcNow,
                    null),
                [],
                [],
                Links(ControlPlaneLinkRelations.Apply))));
        planService.ApplyTenantPlanAssignmentAsync(tenantId, assignmentId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Tenant plan assignment applied."));
        _ctx.Services.AddSingleton(configService);
        _ctx.Services.AddSingleton(planService);

        var cut = _ctx.Render<ControlPlaneTenantConfigurationPage>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Apply assignment", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected apply assignment affordance to render.");
            }
        });

        cut.Find("button[aria-label='Apply tenant plan assignment']").Click();
        await Assert.That(cut.Find("button[aria-label='Confirm tenant plan assignment action']").HasAttribute("disabled")).IsTrue();

        cut.Find("input[aria-label='Assignment confirmation']").Change("APPLY enterprise");
        cut.Find("button[aria-label='Confirm tenant plan assignment action']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Tenant plan assignment applied.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected apply success message.");
            }
        });

        await planService.Received(1).ApplyTenantPlanAssignmentAsync(tenantId, assignmentId, Arg.Any<CancellationToken>());
        await configService.Received(2).GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantConfigPage_RollbackAssignment_RequiresTypedConfirmationAndReloads()
    {
        var tenantId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var configService = Substitute.For<IControlPlaneTenantConfigurationService>();
        var planService = Substitute.For<IControlPlanePlanService>();
        configService.GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneResult.Success(new ControlPlaneTenantEffectiveConfiguration(
                tenantId,
                new ControlPlaneTenantPlanAssignment(
                    assignmentId,
                    tenantId,
                    Guid.NewGuid(),
                    "starter",
                    Guid.NewGuid(),
                    2,
                    4,
                    "Applied",
                    DateTimeOffset.UtcNow,
                    null),
                [],
                [],
                Links(ControlPlaneLinkRelations.Rollback))));
        planService.RollbackTenantPlanAssignmentAsync(tenantId, assignmentId, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Tenant plan assignment rolled back."));
        _ctx.Services.AddSingleton(configService);
        _ctx.Services.AddSingleton(planService);

        var cut = _ctx.Render<ControlPlaneTenantConfigurationPage>(parameters => parameters
            .Add(p => p.TenantId, tenantId));

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Rollback assignment", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected rollback assignment affordance to render.");
            }
        });

        cut.Find("button[aria-label='Rollback tenant plan assignment']").Click();
        await Assert.That(cut.Find("button[aria-label='Confirm tenant plan assignment action']").HasAttribute("disabled")).IsTrue();

        cut.Find("input[aria-label='Assignment confirmation']").Change("ROLLBACK starter");
        cut.Find("button[aria-label='Confirm tenant plan assignment action']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Tenant plan assignment rolled back.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected rollback success message.");
            }
        });

        await planService.Received(1).RollbackTenantPlanAssignmentAsync(tenantId, assignmentId, Arg.Any<CancellationToken>());
        await configService.Received(2).GetEffectiveConfigurationAsync(tenantId, Arg.Any<CancellationToken>());
    }

    private static IReadOnlyDictionary<string, ControlPlaneHalLink> Links(params string[] relations) =>
        relations.ToDictionary(
            relation => relation,
            relation => new ControlPlaneHalLink($"/control-plane/{relation}", "POST"),
            StringComparer.OrdinalIgnoreCase);

    private static bool MatchesEditedDraft(ControlPlaneTenantPlanDraft? draft)
    {
        if (draft is null)
        {
            return false;
        }

        var setting = draft.SettingOverrides.SingleOrDefault();
        var quota = draft.QuotaLimits.SingleOrDefault();

        return draft.Name == "Enterprise Plus"
            && draft.Pricing.Amount == 199.95m
            && draft.Pricing.CurrencyCode == "USD"
            && draft.Pricing.BillingPeriod == "yearly"
            && setting is not null
            && setting.Key == "ai.max_daily_messages"
            && setting.JsonValue == "2000"
            && setting.IsLocked
            && quota is not null
            && quota.Key == "storage.bytes"
            && quota.Limit == 21474836480L;
    }
}
