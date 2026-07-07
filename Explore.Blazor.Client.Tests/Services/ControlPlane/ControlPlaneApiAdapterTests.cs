// ABOUTME: Focused tests for the Blazor host adapter that maps generated control-plane API contracts to the shared RCL contracts.
// ABOUTME: Protects HAL affordance propagation, warning remediation mapping, and safe API error translation.

using Event.ControlPlane.Client.Contracts;
using Explore.Blazor.Client.Services.ControlPlane;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.Client.Tests.Services.ControlPlane;

public sealed class ControlPlaneApiAdapterTests
{
    [Test]
    public async Task GetOverviewAsync_MapsSummaryWarningsAndLinks()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var overview = new HalResourceOfControlPlaneOverviewDto
        {
            DeploymentMode = "MultiTenant",
            Version = "1.2.3",
            PublicOrigin = "https://events.example.test",
            AdminOrigin = "https://admin.example.test",
            TotalTenantCount = 3,
            ActiveTenantCount = 2,
            ProviderSummaries =
            [
                new ProviderSummaries2
                {
                    Key = "smtp",
                    DisplayName = "SMTP",
                    Configured = false,
                    Status = "Missing",
                    Message = "Configure SMTP."
                }
            ],
            Warnings =
            [
                new Warnings6
                {
                    Code = "smtp_missing",
                    Severity = "warning",
                    Message = "SMTP is not configured.",
                    Remediation = "Set SMTP settings."
                }
            ]
        };
        GeneratedHalLinkTestHelper.SetLinks(overview, (ControlPlaneLinkRelations.Self, "/api/admin/control-plane/overview", "GET"));
        apiClient.GetControlPlaneOverviewAsync(null, null, Arg.Any<CancellationToken>()).Returns(overview);
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.GetOverviewAsync();

        await Assert.That(result.Kind).IsEqualTo(ControlPlaneResultKind.Success);
        await Assert.That(result.Value!.DeploymentMode).IsEqualTo("MultiTenant");
        await Assert.That(result.Value.AdminHost).IsEqualTo("https://admin.example.test");
        await Assert.That(result.Value.StatusCards.Count).IsEqualTo(3);
        await Assert.That(result.Value.Warnings.Single().Remediation).IsEqualTo("Set SMTP settings.");
        await Assert.That(result.Value.Links[ControlPlaneLinkRelations.Self].Href).IsEqualTo("/api/admin/control-plane/overview");
    }

    [Test]
    public async Task GetTenantsAsync_MapsEmbeddedItemsAndHalLinks()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var tenant = new HalResourceOfControlPlaneTenantListItemDto
        {
            Id = Guid.NewGuid(),
            FullName = "Central Mosque",
            Slug = "central",
            StatusName = "Active"
        };
        GeneratedHalLinkTestHelper.SetLinks(tenant, (ControlPlaneLinkRelations.Suspend, "/api/admin/control-plane/tenants/central/suspend", "POST"));
        var collection = new HalCollectionResourceOfControlPlaneTenantListItemDto
        {
            TotalCount = 1,
            _embedded = new HalCollectionEmbeddedOfControlPlaneTenantListItemDto { Items = [tenant] }
        };
        GeneratedHalLinkTestHelper.SetLinks(collection, (ControlPlaneLinkRelations.Create, "/api/admin/control-plane/tenants", "POST"));
        apiClient.GetControlPlaneTenantsAsync(null, null, Arg.Any<CancellationToken>()).Returns(collection);
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.GetTenantsAsync();

        await Assert.That(result.Kind).IsEqualTo(ControlPlaneResultKind.Success);
        await Assert.That(result.Value!.TotalCount).IsEqualTo(1);
        await Assert.That(result.Value.Links.ContainsKey(ControlPlaneLinkRelations.Create)).IsTrue();
        await Assert.That(result.Value.Items.Single().Name).IsEqualTo("Central Mosque");
        await Assert.That(result.Value.Items.Single().Links.ContainsKey(ControlPlaneLinkRelations.Suspend)).IsTrue();
    }

    [Test]
    public async Task GetDomainsAsync_MapsDnsRecords()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var domains = new HalResourceOfControlPlaneDomainOverviewDto
        {
            DnsRecords =
            [
                new ControlPlaneDnsRecordDto
                {
                    Name = "admin.example.test",
                    Purpose = "Admin host",
                    Status = "Pending",
                    Target = "control-plane.example.internal",
                    Guidance = "Create a CNAME record."
                }
            ]
        };
        GeneratedHalLinkTestHelper.SetLinks(domains, (ControlPlaneLinkRelations.Self, "/api/admin/control-plane/domains", "GET"));
        apiClient.GetControlPlaneDomainsAsync(null, null, Arg.Any<CancellationToken>()).Returns(domains);
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.GetDomainsAsync();

        await Assert.That(result.Kind).IsEqualTo(ControlPlaneResultKind.Success);
        await Assert.That(result.Value!.Items.Single().Host).IsEqualTo("admin.example.test");
        await Assert.That(result.Value.Items.Single().Purpose).IsEqualTo("Admin host");
        await Assert.That(result.Value.Items.Single().VerificationMessage).IsEqualTo("Create a CNAME record.");
        await Assert.That(result.Value.Links.ContainsKey(ControlPlaneLinkRelations.Self)).IsTrue();
    }

    [Test]
    public async Task GetOperationsAsync_MapsStatusesWarningsMetricsAndLinks()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        var operations = new HalResourceOfControlPlaneOperationsDto
        {
            GeneratedAtUtc = new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero),
            Statuses =
            [
                new ControlPlaneOperationStatusDto
                {
                    Key = "outbox",
                    DisplayName = "Outbox",
                    Status = "Backlog",
                    Severity = "warning",
                    Message = "15 messages are pending.",
                    Metrics =
                    [
                        new ControlPlaneOperationMetricDto
                        {
                            Key = "pending",
                            DisplayName = "Pending",
                            Value = 15,
                            IsCapped = true
                        }
                    ]
                }
            ],
            Warnings =
            [
                new Warnings5
                {
                    Code = "outbox_backlog",
                    Severity = "warning",
                    Message = "Outbox backlog detected.",
                    Remediation = "Inspect the outbox worker."
                }
            ]
        };
        GeneratedHalLinkTestHelper.SetLinks(operations, (ControlPlaneLinkRelations.Self, "/api/admin/control-plane/operations", "GET"));
        apiClient.GetControlPlaneOperationsAsync(null, null, Arg.Any<CancellationToken>()).Returns(operations);
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.GetOperationsAsync();

        await Assert.That(result.Kind).IsEqualTo(ControlPlaneResultKind.Success);
        await Assert.That(result.Value!.Statuses.Single().Label).IsEqualTo("Outbox");
        await Assert.That(result.Value.Statuses.Single().Metrics.Single().IsCapped).IsTrue();
        await Assert.That(result.Value.Warnings.Single().Remediation).IsEqualTo("Inspect the outbox worker.");
        await Assert.That(result.Value.Links.ContainsKey(ControlPlaneLinkRelations.Self)).IsTrue();
    }

    [Test]
    public async Task GetOverviewAsync_WhenApiReturnsForbidden_FailsClosedWithoutResponseLeak()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetControlPlaneOverviewAsync(null, null, Arg.Any<CancellationToken>())
            .Returns<Task<HalResourceOfControlPlaneOverviewDto>>(_ => throw new ApiException(
                "Forbidden",
                403,
                "raw secret response",
                new Dictionary<string, IEnumerable<string>>(),
                null));
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.GetOverviewAsync();

        await Assert.That(result.Kind).IsEqualTo(ControlPlaneResultKind.Forbidden);
        await Assert.That(result.Problem!.StatusCode).IsEqualTo(403);
        await Assert.That(result.Problem.Message).DoesNotContain("raw secret response");
    }

    [Test]
    public async Task ScheduleTenantPurgeAsync_SendsReasonAndConfirmationText()
    {
        var tenantId = Guid.NewGuid();
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.ScheduleControlPlaneTenantPurgeAsync(
                tenantId,
                null,
                null,
                Arg.Is<ControlPlaneTenantLifecycleTransitionRequestDto>(request =>
                    request.Reason == "cleanup" && request.ConfirmationText == "central"),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto
            {
                Success = true,
                Message = "Purge scheduled."
            });
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.ScheduleTenantPurgeAsync(tenantId, "cleanup", "central");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Purge scheduled.");
    }

    [Test]
    public async Task SuspendTenantAsync_WhenApiReturnsConflict_FailsWithoutResponseLeak()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.SuspendControlPlaneTenantAsync(
                Arg.Any<Guid>(),
                null,
                null,
                Arg.Any<ControlPlaneTenantLifecycleTransitionRequestDto>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto>>(_ => throw new ApiException(
                "Conflict",
                409,
                "raw secret response",
                new Dictionary<string, IEnumerable<string>>(),
                null));
        var adapter = new ControlPlaneApiAdapter(apiClient, NullLogger<ControlPlaneApiAdapter>.Instance);

        var result = await adapter.SuspendTenantAsync(Guid.NewGuid(), "maintenance");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.StatusCode).IsEqualTo(409);
        await Assert.That(result.FailureCode).IsEqualTo("control_plane_api_conflict");
        await Assert.That(result.Message).DoesNotContain("raw secret response");
    }
}
