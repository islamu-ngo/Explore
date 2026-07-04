// ABOUTME: bUnit coverage for the shared control-plane overview page embedded in the Blazor client.
// ABOUTME: Proves the host-neutral page renders a safe fail-closed state until an API adapter is registered.

using Event.ControlPlane.Client.Extensions;
using Event.ControlPlane.Client.Contracts;
using Event.ControlPlane.Client.Pages.Domains;
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
            AuthenticationTestConstants.AdminUserId,
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

    private static IReadOnlyDictionary<string, ControlPlaneHalLink> Links(params string[] relations) =>
        relations.ToDictionary(
            relation => relation,
            relation => new ControlPlaneHalLink($"/control-plane/{relation}", "POST"),
            StringComparer.OrdinalIgnoreCase);
}
