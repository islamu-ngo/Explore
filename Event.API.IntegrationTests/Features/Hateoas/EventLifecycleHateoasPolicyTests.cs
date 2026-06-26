// ABOUTME: Direct HAL policy contract tests for event and session lifecycle affordances.
// ABOUTME: Verifies lifecycle links are emitted from server-owned state and route-name constants.

using Explore.API.Hateoas;
using Explore.API.Hateoas.Policies;
using Explore.Application.Authorization;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features.Hateoas;

public sealed class EventLifecycleHateoasPolicyTests
{
    [Test]
    public async Task EventDetailLinks_PointSessionCreationAffordancesToDraftLifecycleRoute()
    {
        var dto = CreateEventDto(EventStatusEnum.Draft);
        var links = new EventDetailLinkPolicy().GetLinks(dto, user: null).ToArray();

        var addSession = links.Single(link => link.Rel == LinkRelations.AddSession);
        var createDraft = links.Single(link => link.Rel == LinkRelations.CreateSessionDraft);

        await Assert.That(addSession.RouteName).IsEqualTo(RouteNames.CreateDraftEventSession);
        await Assert.That(createDraft.RouteName).IsEqualTo(RouteNames.CreateDraftEventSession);
        await Assert.That(createDraft.Method).IsEqualTo("POST");
        await Assert.That(createDraft.RequiresAuth).IsTrue();
        await Assert.That(createDraft.PermissionAction).IsEqualTo(AuthorizationActions.Create);
        await Assert.That(createDraft.PermissionResourceKind).IsEqualTo(ResourceKinds.EventSession);
    }

    [Test]
    public async Task EventDetailLinks_ExposeEventLifecycleActionsByStatus()
    {
        var draftLinks = new EventDetailLinkPolicy().GetLinks(CreateEventDto(EventStatusEnum.Draft), user: null).ToArray();
        var publishedLinks = new EventDetailLinkPolicy().GetLinks(CreateEventDto(EventStatusEnum.Published), user: null).ToArray();
        var cancelledLinks = new EventDetailLinkPolicy().GetLinks(CreateEventDto(EventStatusEnum.Cancelled), user: null).ToArray();

        await Assert.That(draftLinks.Any(link => link.Rel == LinkRelations.PublishReadiness && link.RouteName == RouteNames.GetEventPublishReadiness)).IsTrue();
        await Assert.That(draftLinks.Any(link => link.Rel == LinkRelations.Publish && link.RouteName == RouteNames.PublishEvent)).IsTrue();
        await Assert.That(draftLinks.Any(link => link.Rel == LinkRelations.Cancel && link.RouteName == RouteNames.CancelEvent)).IsTrue();
        await Assert.That(draftLinks.Any(link => link.Rel == LinkRelations.Archive && link.RouteName == RouteNames.ArchiveEvent)).IsTrue();
        await Assert.That(publishedLinks.Any(link => link.Rel == LinkRelations.Cancel && link.RouteName == RouteNames.CancelEvent)).IsTrue();
        await Assert.That(publishedLinks.Any(link => link.Rel == LinkRelations.Publish)).IsFalse();
        await Assert.That(cancelledLinks.Any(link => link.Rel == LinkRelations.Archive && link.RouteName == RouteNames.ArchiveEvent)).IsTrue();
    }

    [Test]
    public async Task EventSessionDetailLinks_ExposeScheduleAndPublishFromSessionState()
    {
        var unscheduledDraftLinks = new EventSessionDetailLinkPolicy()
            .GetLinks(CreateSessionDto(EventSessionStatusEnum.Draft, isScheduled: false), user: null)
            .ToArray();
        var scheduledDraftLinks = new EventSessionDetailLinkPolicy()
            .GetLinks(CreateSessionDto(EventSessionStatusEnum.Draft, isScheduled: true), user: null)
            .ToArray();
        var archivedLinks = new EventSessionDetailLinkPolicy()
            .GetLinks(CreateSessionDto(EventSessionStatusEnum.Archived, isScheduled: true), user: null)
            .ToArray();
        var publishedLinks = new EventSessionDetailLinkPolicy()
            .GetLinks(CreateSessionDto(EventSessionStatusEnum.Published, isScheduled: true), user: null)
            .ToArray();
        var cancelledLinks = new EventSessionDetailLinkPolicy()
            .GetLinks(CreateSessionDto(EventSessionStatusEnum.Cancelled, isScheduled: true), user: null)
            .ToArray();
        var moderatedLinks = new EventSessionDetailLinkPolicy()
            .GetLinks(CreateSessionDto(EventSessionStatusEnum.Moderated, isScheduled: true), user: null)
            .ToArray();

        await Assert.That(unscheduledDraftLinks.Any(link => link.Rel == LinkRelations.Schedule && link.RouteName == RouteNames.ScheduleEventSession)).IsTrue();
        await Assert.That(unscheduledDraftLinks.Any(link => link.Rel == LinkRelations.Publish)).IsFalse();
        await Assert.That(scheduledDraftLinks.Any(link => link.Rel == LinkRelations.Publish && link.RouteName == RouteNames.PublishEventSession)).IsTrue();
        await Assert.That(scheduledDraftLinks.Any(link => link.Rel == LinkRelations.Cancel && link.RouteName == RouteNames.CancelEventSession)).IsTrue();
        await Assert.That(publishedLinks.Any(link => link.Rel == LinkRelations.Complete && link.RouteName == RouteNames.CompleteEventSession)).IsTrue();
        await Assert.That(publishedLinks.Any(link => link.Rel == LinkRelations.Cancel && link.RouteName == RouteNames.CancelEventSession)).IsTrue();
        await Assert.That(cancelledLinks.Any(link => link.Rel == LinkRelations.Archive && link.RouteName == RouteNames.ArchiveEventSession)).IsTrue();
        await Assert.That(archivedLinks.Any(link => link.Rel == LinkRelations.Schedule)).IsFalse();
        await Assert.That(archivedLinks.Any(link => link.Rel == LinkRelations.Publish)).IsFalse();
        await Assert.That(moderatedLinks.Any(link => link.Rel is LinkRelations.Schedule or LinkRelations.Publish or LinkRelations.Cancel or LinkRelations.Complete or LinkRelations.Archive)).IsFalse();
        await Assert.That(moderatedLinks.Any(link => link.Rel.StartsWith("moderate", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task EventSessionCollectionLinks_ExposeLifecycleItemAffordancesFromListState()
    {
        var links = new EventSessionCollectionLinkPolicy()
            .GetItemLinks(CreateSessionListDto(EventSessionStatusEnum.Draft, isScheduled: true), user: null)
            .ToArray();

        var schedule = links.Single(link => link.Rel == LinkRelations.Schedule);
        var publish = links.Single(link => link.Rel == LinkRelations.Publish);

        await Assert.That(schedule.RouteName).IsEqualTo(RouteNames.ScheduleEventSession);
        await Assert.That(schedule.PermissionResourceKind).IsEqualTo(ResourceKinds.EventSession);
        await Assert.That(schedule.PermissionAction).IsEqualTo(AuthorizationActions.Update);
        await Assert.That(publish.RouteName).IsEqualTo(RouteNames.PublishEventSession);
        await Assert.That(publish.PermissionResourceKind).IsEqualTo(ResourceKinds.EventSession);
        await Assert.That(publish.PermissionAction).IsEqualTo(AuthorizationActions.Update);
    }

    private static EventDto CreateEventDto(EventStatusEnum status) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Lifecycle Event",
        ActorId = Guid.NewGuid(),
        ActorDisplayName = "Organizer",
        ActorTypeFullName = "User",
        EventStatusId = (int)status,
        EventStatusFullName = status.ToString(),
        EventStatusMasterCode = status.ToString(),
        VisibilityTypeFullName = "Public",
        VisibilityTypeMasterCode = "public",
        EventFormatFullName = "In Person",
        EventFormatMasterCode = "in_person",
        TenantId = Guid.NewGuid()
    };

    private static EventSessionDto CreateSessionDto(EventSessionStatusEnum status, bool isScheduled) => new()
    {
        Id = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        EventTitle = "Lifecycle Event",
        Title = "Lifecycle Session",
        EventSessionStatusId = (int)status,
        IsScheduled = isScheduled,
        TenantId = Guid.NewGuid()
    };

    private static EventSessionListDto CreateSessionListDto(EventSessionStatusEnum status, bool isScheduled) => new()
    {
        Id = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        EventTitle = "Lifecycle Event",
        Title = "Lifecycle Session",
        EventSessionStatusId = (int)status,
        IsScheduled = isScheduled,
        TenantId = Guid.NewGuid()
    };
}
