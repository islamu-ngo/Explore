// ABOUTME: HATEOAS contract tests for EmailDispatch operator status affordances.
// ABOUTME: Protects replay and park links from drifting away from durable dispatch state rules.

using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.EmailDispatch;
using Explore.Application.Hateoas;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

[Category(TestCategories.Email)]
public sealed class EmailDispatchAdminHateoasTests
{
    [Test]
    public async Task DeferredStatusRows_ExposeReplayAndParkLinks()
    {
        var tenantId = Guid.NewGuid();
        var outboxId = Guid.NewGuid();
        var dto = CreateStatus(tenantId, outboxId, "DeadLettered");
        var policy = new EmailDispatchStatusCollectionLinkPolicy();

        var links = policy.GetItemLinks(dto, user: null).ToList();

        var replay = links.Single(link => link.Rel == "replay");
        await Assert.That(replay.RouteName).IsEqualTo(RouteNames.ReplayEmailDispatch);
        await Assert.That(replay.Method).IsEqualTo("POST");
        await Assert.That(replay.RequiresAuth).IsTrue();
        await Assert.That(replay.PermissionResourceKind).IsEqualTo(ResourceKinds.EmailDispatch);
        await Assert.That(replay.PermissionAction).IsEqualTo(AuthorizationActions.EmailDispatches.Replay);
        await Assert.That(replay.PermissionResourceId).IsEqualTo(outboxId.ToString());
        await Assert.That(replay.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
        await AssertPermissionAttributes(replay, tenantId, outboxId, dto);
        await Assert.That(GetRouteValue<Guid>(replay.RouteValues, "tenantId")).IsEqualTo(tenantId);
        await Assert.That(GetRouteValue<Guid>(replay.RouteValues, "outboxId")).IsEqualTo(outboxId);

        var park = links.Single(link => link.Rel == "park");
        await Assert.That(park.RouteName).IsEqualTo(RouteNames.ParkEmailDispatch);
        await Assert.That(park.Method).IsEqualTo("PUT");
        await Assert.That(park.RequiresAuth).IsTrue();
        await Assert.That(park.PermissionResourceKind).IsEqualTo(ResourceKinds.EmailDispatch);
        await Assert.That(park.PermissionAction).IsEqualTo(AuthorizationActions.EmailDispatches.Park);
        await Assert.That(park.PermissionResourceId).IsEqualTo(outboxId.ToString());
        await Assert.That(park.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
        await AssertPermissionAttributes(park, tenantId, outboxId, dto);
        await Assert.That(GetRouteValue<Guid>(park.RouteValues, "tenantId")).IsEqualTo(tenantId);
        await Assert.That(GetRouteValue<Guid>(park.RouteValues, "outboxId")).IsEqualTo(outboxId);

        var resolve = links.Single(link => link.Rel == "resolve-without-replay");
        await Assert.That(resolve.RouteName).IsEqualTo(RouteNames.ResolveEmailDispatchWithoutReplay);
        await Assert.That(resolve.Method).IsEqualTo("POST");
        await Assert.That(resolve.PermissionAction).IsEqualTo(AuthorizationActions.EmailDispatches.Resolve);
        await AssertPermissionAttributes(resolve, tenantId, outboxId, dto);
    }

    [Test]
    public async Task SentStatusRows_DoNotExposeReplayOrParkLinks()
    {
        var policy = new EmailDispatchStatusCollectionLinkPolicy();

        var links = policy.GetItemLinks(CreateStatus(Guid.NewGuid(), Guid.NewGuid(), "Sent"), user: null).ToList();

        await Assert.That(links.Any(link => link.Rel == "replay")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "park")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "resolve-without-replay")).IsFalse();
    }

    [Test]
    public async Task ParkedStatusRows_ExposeReplayButNotParkLink()
    {
        var policy = new EmailDispatchStatusCollectionLinkPolicy();

        var links = policy.GetItemLinks(CreateStatus(Guid.NewGuid(), Guid.NewGuid(), "Parked"), user: null).ToList();

        await Assert.That(links.Any(link => link.Rel == "replay")).IsTrue();
        await Assert.That(links.Any(link => link.Rel == "park")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "resolve-without-replay")).IsTrue();
    }

    [Test]
    public async Task ProcessingStatusRows_ExposeParkButNotReplayLink()
    {
        var policy = new EmailDispatchStatusCollectionLinkPolicy();

        var links = policy.GetItemLinks(CreateStatus(Guid.NewGuid(), Guid.NewGuid(), "Processing"), user: null).ToList();

        await Assert.That(links.Any(link => link.Rel == "replay")).IsFalse();
        await Assert.That(links.Any(link => link.Rel == "park")).IsTrue();
        await Assert.That(links.Any(link => link.Rel == "resolve-without-replay")).IsFalse();
    }

    [Test]
    public async Task RedactedStatusRowsExposeNoMutationLinks()
    {
        var policy = new EmailDispatchStatusCollectionLinkPolicy();
        var dto = CreateStatus(Guid.NewGuid(), Guid.NewGuid(), "DeadLettered");
        dto.ContentRedactedAt = DateTime.UtcNow;

        var links = policy.GetItemLinks(dto, user: null).ToList();

        await Assert.That(links).IsEmpty();
    }

    private static EmailDispatchStatusDto CreateStatus(Guid tenantId, Guid outboxId, string deliveryStatus) => new()
    {
        TenantId = tenantId,
        OutboxId = outboxId,
        SourceType = "event_registration",
        SourceId = Guid.NewGuid(),
        DeliveryStatus = deliveryStatus,
        AttemptCount = 1,
        CorrelationId = Guid.NewGuid().ToString("N")
    };

    private static T? GetRouteValue<T>(object? routeValues, string name)
    {
        if (routeValues is null)
            return default;

        var property = routeValues.GetType().GetProperty(name);
        var value = property?.GetValue(routeValues);
        return value is T typedValue ? typedValue : default;
    }

    private static async Task AssertPermissionAttributes(
        LinkDefinition link,
        Guid tenantId,
        Guid outboxId,
        EmailDispatchStatusDto dto)
    {
        await Assert.That(link.PermissionResourceAttributes).IsNotNull();
        await Assert.That(link.PermissionResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(link.PermissionResourceAttributes["outboxId"]).IsEqualTo(outboxId.ToString());
        await Assert.That(link.PermissionResourceAttributes["sourceType"]).IsEqualTo(dto.SourceType);
        await Assert.That(link.PermissionResourceAttributes["sourceId"]).IsEqualTo(dto.SourceId.ToString());
        await Assert.That(link.PermissionResourceAttributes["deliveryStatus"]).IsEqualTo(dto.DeliveryStatus);
    }
}
