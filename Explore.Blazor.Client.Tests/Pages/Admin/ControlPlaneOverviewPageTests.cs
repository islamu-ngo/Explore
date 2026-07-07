// ABOUTME: bUnit coverage for the shared control-plane overview page embedded in the Blazor client.
// ABOUTME: Proves the host-neutral page renders a safe fail-closed state until an API adapter is registered.

using Event.ControlPlane.Client.Contracts;
using Event.ControlPlane.Client.Extensions;
using Event.ControlPlane.Client.Pages.Domains;
using Event.ControlPlane.Client.Pages.Operations;
using Event.ControlPlane.Client.Pages.Overview;
using Event.ControlPlane.Client.Pages.Tenants;
using Event.ControlPlane.Client.Services;

namespace Explore.Blazor.Client.Tests.Pages.Admin;

public sealed class ControlPlaneOverviewPageTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public ControlPlaneOverviewPageTests()
    {
        _ctx.Services.AddEventControlPlaneClient();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task Render_WhenNoHostApiAdapterRegistered_ShowsFailClosedState()
    {
        var cut = _ctx.Render<ControlPlaneOverviewPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Control-plane API unavailable", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected the control-plane overview to render its fail-closed state.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Event Control Plane");
        await Assert.That(cut.Markup).Contains("The control-plane API adapter is not configured for this host.");
    }

    [Test]
    public async Task TenantsPage_RendersOnlyHalAdvertisedActions()
    {
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [
                new ControlPlaneTenantSummary(
                    Guid.NewGuid(),
                    "Active Mosque",
                    "active-mosque",
                    "Active",
                    "active.example.test",
                    4096,
                    Links(ControlPlaneLinkRelations.Suspend)),
                new ControlPlaneTenantSummary(
                    Guid.NewGuid(),
                    "Quiet Mosque",
                    "quiet-mosque",
                    "Active")
            ],
            2,
            Links(ControlPlaneLinkRelations.Create))));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Active Mosque", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant inventory to render.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Tenants");
        await Assert.That(cut.Markup).Contains("Create tenant");
        await Assert.That(cut.Markup).Contains("Suspend");
        await Assert.That(cut.Markup).DoesNotContain("Archive");
        await Assert.That(cut.Markup).DoesNotContain("Schedule purge");
    }

    [Test]
    public async Task TenantsPage_WithAdminClaimsButNoHalLinks_HidesActions()
    {
        _ctx.SetAuthenticatedUserWithClaims(
            Guid.NewGuid(),
            "Instance Admin",
            new Claim("explore:admin:instance", "true"));

        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(Guid.NewGuid(), "Active Mosque", "active-mosque", "Active")],
            1)));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Active Mosque", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant inventory to render.");
            }
        });

        await Assert.That(cut.Markup).DoesNotContain("Create tenant");
        await Assert.That(cut.Markup).DoesNotContain("Suspend");
        await Assert.That(cut.Markup).DoesNotContain("Archive");
    }

    [Test]
    public async Task TenantsPage_SuspendHalAction_CallsTenantCommandAndShowsSuccess()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(tenantId, "Active Mosque", "active-mosque", "Active", Links: Links(ControlPlaneLinkRelations.Suspend))],
            1)));
        tenantService.SuspendTenantAsync(tenantId, null, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Tenant suspended."));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Suspend", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant suspend affordance to render.");
            }
        });

        cut.Find("button[aria-label='Suspend Active Mosque']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Tenant suspended.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected command success message to render.");
            }
        });

        await tenantService.Received(1).SuspendTenantAsync(tenantId, null, Arg.Any<CancellationToken>());
        await tenantService.Received(2).GetTenantsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantsPage_CommandFailure_ShowsSafeFailureAndDoesNotReload()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(tenantId, "Active Mosque", "active-mosque", "Active", Links: Links(ControlPlaneLinkRelations.Archive))],
            1)));
        tenantService.ArchiveTenantAsync(tenantId, null, Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Failed("Archive requires an operator reason.", "control_plane_validation_failed", 400));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Archive", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant archive affordance to render.");
            }
        });

        cut.Find("button[aria-label='Archive Active Mosque']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Archive requires an operator reason.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected safe command failure message to render.");
            }
        });

        await tenantService.Received(1).ArchiveTenantAsync(tenantId, null, Arg.Any<CancellationToken>());
        await tenantService.Received(1).GetTenantsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TenantsPage_PurgeHalAction_RequiresReasonAndExactSlugBeforeConfirm()
    {
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(Guid.NewGuid(), "Archived Mosque", "archived-mosque", "Archived", Links: Links(ControlPlaneLinkRelations.Purge))],
            1)));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Schedule purge", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant purge affordance to render.");
            }
        });

        cut.Find("button[aria-label='Schedule purge for Archived Mosque']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Confirm tenant purge", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant purge confirmation to render.");
            }
        });

        await Assert.That(cut.Find("button[aria-label='Confirm purge for Archived Mosque']").HasAttribute("disabled")).IsTrue();

        cut.Find("input[aria-label='Purge reason']").Change("cleanup complete");
        cut.Find("input[aria-label='Purge confirmation']").Change("wrong-slug");
        await Assert.That(cut.Find("button[aria-label='Confirm purge for Archived Mosque']").HasAttribute("disabled")).IsTrue();

        cut.Find("input[aria-label='Purge confirmation']").Change("archived-mosque");
        await Assert.That(cut.Find("button[aria-label='Confirm purge for Archived Mosque']").HasAttribute("disabled")).IsFalse();
    }

    [Test]
    public async Task TenantsPage_ConfirmedPurge_CallsCommandShowsSuccessAndReloads()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(tenantId, "Archived Mosque", "archived-mosque", "Archived", Links: Links(ControlPlaneLinkRelations.Purge))],
            1)));
        tenantService.ScheduleTenantPurgeAsync(tenantId, "cleanup complete", "archived-mosque", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Succeeded("Purge scheduled."));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Schedule purge", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant purge affordance to render.");
            }
        });

        cut.Find("button[aria-label='Schedule purge for Archived Mosque']").Click();
        cut.Find("input[aria-label='Purge reason']").Change("cleanup complete");
        cut.Find("input[aria-label='Purge confirmation']").Change("archived-mosque");
        cut.Find("button[aria-label='Confirm purge for Archived Mosque']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Purge scheduled.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected purge success message to render.");
            }
        });

        await tenantService.Received(1).ScheduleTenantPurgeAsync(tenantId, "cleanup complete", "archived-mosque", Arg.Any<CancellationToken>());
        await tenantService.Received(2).GetTenantsAsync(Arg.Any<CancellationToken>());
        await Assert.That(cut.Markup).DoesNotContain("Confirm tenant purge");
    }

    [Test]
    public async Task TenantsPage_CancelPurge_ClosesConfirmationWithoutCommand()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(tenantId, "Archived Mosque", "archived-mosque", "Archived", Links: Links(ControlPlaneLinkRelations.Purge))],
            1)));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Schedule purge", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant purge affordance to render.");
            }
        });

        cut.Find("button[aria-label='Schedule purge for Archived Mosque']").Click();
        cut.Find("button[aria-label='Cancel tenant purge']").Click();

        await tenantService.DidNotReceive().ScheduleTenantPurgeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await Assert.That(cut.Markup).DoesNotContain("Confirm tenant purge");
    }

    [Test]
    public async Task TenantsPage_PurgeFailure_ShowsSafeFailureDoesNotReloadAndKeepsConfirmation()
    {
        var tenantId = Guid.NewGuid();
        var tenantService = Substitute.For<IControlPlaneTenantService>();
        tenantService.GetTenantsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneTenantList(
            [new ControlPlaneTenantSummary(tenantId, "Archived Mosque", "archived-mosque", "Archived", Links: Links(ControlPlaneLinkRelations.Purge))],
            1)));
        tenantService.ScheduleTenantPurgeAsync(tenantId, "cleanup complete", "archived-mosque", Arg.Any<CancellationToken>())
            .Returns(ControlPlaneCommandResult.Failed("Purge requires fresh confirmation.", "control_plane_validation_failed", 400));
        _ctx.Services.AddSingleton(tenantService);

        var cut = _ctx.Render<ControlPlaneTenantsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Schedule purge", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected tenant purge affordance to render.");
            }
        });

        cut.Find("button[aria-label='Schedule purge for Archived Mosque']").Click();
        cut.Find("input[aria-label='Purge reason']").Change("cleanup complete");
        cut.Find("input[aria-label='Purge confirmation']").Change("archived-mosque");
        cut.Find("button[aria-label='Confirm purge for Archived Mosque']").Click();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Purge requires fresh confirmation.", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected safe purge failure message to render.");
            }
        });

        await tenantService.Received(1).ScheduleTenantPurgeAsync(tenantId, "cleanup complete", "archived-mosque", Arg.Any<CancellationToken>());
        await tenantService.Received(1).GetTenantsAsync(Arg.Any<CancellationToken>());
        await Assert.That(cut.Markup).Contains("Confirm tenant purge");
    }

    [Test]
    public async Task DomainsPage_RendersOnlyHalAdvertisedActions()
    {
        var domainService = Substitute.For<IControlPlaneDomainService>();
        domainService.GetDomainsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneDomainList(
            [
                new ControlPlaneDomainSummary(
                    "admin.example.test",
                    "Admin",
                    "Pending",
                    "control-plane.example.internal",
                    "DNS TXT record is pending.",
                    Links(ControlPlaneLinkRelations.Verify, ControlPlaneLinkRelations.Test)),
                new ControlPlaneDomainSummary("public.example.test", "Public", "Verified")
            ])));
        _ctx.Services.AddSingleton(domainService);

        var cut = _ctx.Render<ControlPlaneDomainsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("admin.example.test", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected domain inventory to render.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Domains");
        await Assert.That(cut.Markup).Contains("Verify");
        await Assert.That(cut.Markup).Contains("Test");
        await Assert.That(cut.Markup).DoesNotContain("Retry");
    }

    [Test]
    public async Task OperationsPage_WhenNoHostApiAdapterRegistered_ShowsFailClosedState()
    {
        var cut = _ctx.Render<ControlPlaneOperationsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Control-plane API unavailable", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected operations page to render its fail-closed state.");
            }
        });

        await Assert.That(cut.Find("h1").TextContent).IsEqualTo("Operations");
        await Assert.That(cut.Markup).Contains("The control-plane API adapter is not configured for this host.");
    }

    [Test]
    public async Task OperationsPage_RendersStatusesWarningsAndMetrics()
    {
        var operationsService = Substitute.For<IControlPlaneOperationsService>();
        operationsService.GetOperationsAsync(Arg.Any<CancellationToken>()).Returns(ControlPlaneResult.Success(new ControlPlaneOperations(
            new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero),
            [
                new ControlPlaneOperationStatus(
                    "outbox",
                    "Outbox",
                    "Backlog",
                    ControlPlaneSeverity.Warning,
                    "15 messages are pending.",
                    [new ControlPlaneOperationMetric("pending", "Pending", 15, true)])
            ],
            [new ControlPlaneWarning("outbox_backlog", "Outbox backlog detected.", Remediation: "Inspect the outbox worker.")],
            Links(ControlPlaneLinkRelations.Self))));
        _ctx.Services.AddSingleton(operationsService);

        var cut = _ctx.Render<ControlPlaneOperationsPage>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Outbox", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Expected operations status to render.");
            }
        });

        await Assert.That(cut.Markup).Contains("Outbox backlog detected.");
        await Assert.That(cut.Markup).Contains("Inspect the outbox worker.");
        await Assert.That(cut.Markup).Contains("Pending");
        await Assert.That(cut.Markup).Contains("capped");
    }

    private static IReadOnlyDictionary<string, ControlPlaneHalLink> Links(params string[] relations) =>
        relations.ToDictionary(
            relation => relation,
            relation => new ControlPlaneHalLink($"/control-plane/{relation}", "POST"),
            StringComparer.OrdinalIgnoreCase);
}
