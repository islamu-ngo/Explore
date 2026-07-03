// ABOUTME: Unit-level HATEOAS policy tests for event detail affordance metadata.
// ABOUTME: Guards add-session authorization context used by Cerbos/local parity checks.

namespace Event.Api.IntegrationTests.Features.Hateoas;

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Routing;
using TUnit.Core;

public sealed class EventLinkPolicyTests
{
    [Test]
    public async Task AddSessionLinks_UseEventSessionPreCreateAuthorizationContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var dto = CreateEventDto(eventId, tenantId, Guid.NewGuid());

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        foreach (var rel in new[] { LinkRelations.AddSession, LinkRelations.SessionCreateContext })
        {
            var link = links.Single(definition => definition.Rel == rel);

            await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.EventSession);
            await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Create);
            await Assert.That(link.PermissionResourceId).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes).IsNotNull();
            await Assert.That(link.PermissionResourceAttributes!["tenantId"]).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes["eventId"]).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionResourceAttributes["authorizationPhase"]).IsEqualTo(AuthorizationPhases.PreCreate);
        }
    }

    [Test]
    public async Task DraftEventLifecycleLinks_UseEventAuthorizationContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(eventId, tenantId, organizationId);

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        foreach (var rel in new[] { LinkRelations.Publish, LinkRelations.Cancel, LinkRelations.Archive })
        {
            var link = links.Single(definition => definition.Rel == rel);

            await Assert.That(link.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
            await Assert.That(link.PermissionAction).IsEqualTo(AuthorizationActions.Update);
            await Assert.That(link.PermissionResourceId).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionScope?.TenantId).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes).IsNotNull();
            await Assert.That(link.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
            await Assert.That(link.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
            await Assert.That(link.PermissionResourceAttributes["organizationId"]).IsEqualTo(organizationId.ToString());
        }
    }

    [Test]
    public async Task DraftUserOwnedEventLifecycleLinks_IncludeUserOwnerAuthorizationContext()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = CreateEventDto(eventId, tenantId, organizationId: null, userId);

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var publish = links.Single(definition => definition.Rel == LinkRelations.Publish);

        await Assert.That(publish.PermissionResourceAttributes).IsNotNull();
        await Assert.That(publish.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(publish.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
        await Assert.That(publish.PermissionResourceAttributes["userId"]).IsEqualTo(userId.ToString());
        await Assert.That(publish.PermissionResourceAttributes.ContainsKey("organizationId")).IsFalse();
    }

    [Test]
    public async Task LightModerationLink_UsesLightModerationAuthorizationAction()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var moderate = links.Single(definition => definition.Rel == LinkRelations.ModerateLight);

        await Assert.That(moderate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(moderate.RouteName).IsEqualTo(RouteNames.ModerateEventLight);
        await Assert.That(moderate.PermissionAction).IsEqualTo(AuthorizationActions.Events.ModerateLight);
        await Assert.That(moderate.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes).IsNotNull();
        await Assert.That(moderate.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }

    [Test]
    public async Task PublishedEvent_AdvertisesHeavyModerationAuthorizationAction()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var moderate = links.Single(definition => definition.Rel == LinkRelations.ModerateHeavy);

        await Assert.That(moderate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(moderate.RouteName).IsEqualTo(RouteNames.ModerateEventHeavy);
        await Assert.That(moderate.PermissionAction).IsEqualTo(AuthorizationActions.Events.ModerateHeavy);
        await Assert.That(moderate.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes).IsNotNull();
        await Assert.That(moderate.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(moderate.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }

    [Test]
    public async Task DraftEvent_DoesNotAdvertiseLightOrUnmoderate()
    {
        var dto = CreateEventDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ModerateLight)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Unmoderate)).IsFalse();
    }

    [Test]
    public async Task ModeratedEligibleEvent_AdvertisesUnmoderateAuthorizationAction()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsUnmoderationEligible = true;

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var unmoderate = links.Single(definition => definition.Rel == LinkRelations.Unmoderate);

        await Assert.That(unmoderate.PermissionResourceKind).IsEqualTo(ResourceKinds.Event);
        await Assert.That(unmoderate.RouteName).IsEqualTo(RouteNames.UnmoderateEvent);
        await Assert.That(unmoderate.PermissionAction).IsEqualTo(AuthorizationActions.Events.Unmoderate);
        await Assert.That(unmoderate.PermissionResourceId).IsEqualTo(eventId.ToString());
        await Assert.That(unmoderate.PermissionResourceAttributes).IsNotNull();
        await Assert.That(unmoderate.PermissionResourceAttributes!["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(unmoderate.PermissionResourceAttributes["tenantId"]).IsEqualTo(tenantId.ToString());
    }

    [Test]
    public async Task ModeratedIneligibleEvent_DoesNotAdvertiseUnmoderate()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsUnmoderationEligible = false;

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Unmoderate)).IsFalse();
    }

    [Test]
    public async Task IrreversiblyModeratedEvent_DoesNotAdvertiseModerationActions()
    {
        var dto = CreateEventDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Moderated,
            statusName: "Moderated",
            statusCode: "MODERATED");
        dto.IsUnmoderationEligible = false;

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ModerateLight)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ModerateHeavy)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Unmoderate)).IsFalse();
    }

    [Test]
    public async Task PublishedEventLifecycleLinks_ExposeCancelButNotDraftActions()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            tenantId,
            organizationId,
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Cancel)).IsTrue();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Publish)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.Archive)).IsFalse();
    }

    [Test]
    public async Task PublishedEvent_AdvertisesReporterFacingReportAffordances()
    {
        var eventId = Guid.NewGuid();
        var dto = CreateEventDto(
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            status: EventStatusEnum.Published,
            statusName: "Published",
            statusCode: "PUBLISHED");

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        var options = links.Single(definition => definition.Rel == LinkRelations.EventReportOptions);
        await Assert.That(options.RouteName).IsEqualTo(RouteNames.GetEventReportOptions);
        await Assert.That(new RouteValueDictionary(options.RouteValues)["eventId"]).IsEqualTo(eventId);
        await Assert.That(options.RequiresAuth).IsFalse();

        var submit = links.Single(definition => definition.Rel == LinkRelations.ReportEvent);
        await Assert.That(submit.RouteName).IsEqualTo(RouteNames.SubmitEventReport);
        await Assert.That(submit.Method).IsEqualTo("POST");
        await Assert.That(submit.RequiresAuth).IsTrue();
        await Assert.That(submit.AdvertiseWhenAnonymous).IsTrue();
    }

    [Test]
    public async Task DraftEvent_DoesNotAdvertiseReportAffordances()
    {
        var dto = CreateEventDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var links = new EventDetailLinkPolicy()
            .GetLinks(dto, new ClaimsPrincipal(new ClaimsIdentity("test")))
            .ToList();

        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.EventReportOptions)).IsFalse();
        await Assert.That(links.Any(definition => definition.Rel == LinkRelations.ReportEvent)).IsFalse();
    }

    private static EventDto CreateEventDto(
        Guid eventId,
        Guid tenantId,
        Guid? organizationId,
        Guid? userId = null,
        EventStatusEnum status = EventStatusEnum.Draft,
        string statusName = "Draft",
        string statusCode = "DRAFT") => new()
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Program launch",
            ActorId = Guid.NewGuid(),
            ActorDisplayName = "ISLAMU",
            ActorTypeId = userId.HasValue ? (int)ActorTypeEnum.User : (int)ActorTypeEnum.Organization,
            ActorTypeFullName = userId.HasValue ? "User" : "Organization",
            ActorUserId = userId,
            ActorOrganizationId = organizationId,
            EventStatusId = (int)status,
            EventStatusFullName = statusName,
            EventStatusMasterCode = statusCode,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityTypeFullName = "Public",
            VisibilityTypeMasterCode = "PUBLIC",
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormatFullName = "In person",
            EventFormatMasterCode = "IN_PERSON"
        };
}
