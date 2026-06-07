// ABOUTME: Unit tests for route/workflow-scoped AI tool catalog views.
// ABOUTME: Proves catalog visibility remains separate from HAL/API execution authority.

using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Event.Application.UnitTests.Features.AiAssistant.Tools;

public sealed class AiToolCatalogServiceTests
{
    [Test]
    public async Task GetCatalogWhenRouteWorkflowContextAndHalMatchReturnsProposalAvailableItem()
    {
        var item = new AiToolCatalogService().GetCatalog(CreateQuery(halRels: HalSet("create-event"))).Single();

        await Assert.That(item.Kind).IsEqualTo(AiProposedActionKind.CreateEventDraft);
        await Assert.That(item.CanRequestProposal).IsTrue();
        await Assert.That(item.ExecutionAuthorityGranted).IsFalse();
        await Assert.That(item.AvailabilityCode).IsEqualTo(AiToolCatalogAvailabilityCodes.Available);
    }

    [Test]
    public async Task GetCatalogWhenHalAffordanceDiffersByCaseStillReturnsAvailableItem()
    {
        var item = new AiToolCatalogService().GetCatalog(CreateQuery(halRels: HalSet("CREATE-EVENT"))).Single();

        await Assert.That(item.CanRequestProposal).IsTrue();
        await Assert.That(item.AvailabilityCode).IsEqualTo(AiToolCatalogAvailabilityCodes.Available);
    }

    [Test]
    public async Task GetCatalogWhenHalAffordanceIsMissingKeepsItemNonExecutable()
    {
        var item = new AiToolCatalogService().GetCatalog(CreateQuery()).Single();

        await Assert.That(item.CanRequestProposal).IsFalse();
        await Assert.That(item.ExecutionAuthorityGranted).IsFalse();
        await Assert.That(item.AvailabilityCode).IsEqualTo(AiToolCatalogAvailabilityCodes.MissingHalAffordance);
        await Assert.That(item.AvailabilityReason).Contains("API/HAL");
    }

    [Test]
    public async Task GetCatalogWhenRouteDoesNotMatchReturnsNoItems()
    {
        var catalog = new AiToolCatalogService().GetCatalog(CreateQuery(routePath: "/admin/settings", halRels: HalSet("create-event")));

        await Assert.That(catalog).IsEmpty();
    }

    [Test]
    public async Task GetCatalogWhenTenantIsMissingReturnsNoItems()
    {
        var catalog = new AiToolCatalogService().GetCatalog(CreateQuery(tenantId: Guid.Empty, halRels: HalSet("create-event")));

        await Assert.That(catalog).IsEmpty();
    }

    [Test]
    public async Task GetCatalogWhenPrincipalIsAnonymousReturnsNoItems()
    {
        var catalog = new AiToolCatalogService().GetCatalog(CreateQuery(isAuthenticated: false, halRels: HalSet("create-event")));

        await Assert.That(catalog).IsEmpty();
    }

    private static HashSet<string> HalSet(params string[] rels) => new(rels, StringComparer.OrdinalIgnoreCase);

    private static AiToolCatalogQuery CreateQuery(
        Guid? tenantId = null,
        bool isAuthenticated = true,
        string routePath = "/events",
        string workflowScope = "event-drafting",
        string contextScope = "selected-references",
        IReadOnlySet<string>? halRels = null)
        => new(
            tenantId ?? Guid.CreateVersion7(),
            isAuthenticated,
            AiToolCatalogPrincipalKind.User,
            routePath,
            workflowScope,
            contextScope,
            halRels ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase));
}
