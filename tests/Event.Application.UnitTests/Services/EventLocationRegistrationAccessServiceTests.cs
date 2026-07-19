// ABOUTME: Exhaustive registration-state and scope tests for EventLocation attendee access.
// ABOUTME: Proves fail-closed lifecycle handling, null-approval modes, audience ceilings, and repository separation.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Event.Application.UnitTests.Services;

public sealed class EventLocationRegistrationAccessServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid IntentId = Guid.CreateVersion7();
    private static readonly Guid EventId = Guid.CreateVersion7();
    private static readonly Guid DayId = Guid.CreateVersion7();
    private static readonly Guid SessionId = Guid.CreateVersion7();
    private static readonly Guid EventLocationId = Guid.CreateVersion7();
    private readonly EventLocationRegistrationAccessService _service = new();

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)RegistrationModeEnum.Open, EventLocationRegistrationEffectiveState.Confirmed, true)]
    [Arguments((int)RegistrationModeEnum.ApprovalRequired, EventLocationRegistrationEffectiveState.Pending, true)]
    [Arguments((int)RegistrationModeEnum.InviteOnly, EventLocationRegistrationEffectiveState.Denied, false)]
    [Arguments((int)RegistrationModeEnum.Closed, EventLocationRegistrationEffectiveState.Denied, false)]
    [Arguments(null, EventLocationRegistrationEffectiveState.Denied, false)]
    [Arguments(999, EventLocationRegistrationEffectiveState.Denied, false)]
    public async Task Resolve_NullApproval_UsesRegistrationModeAndFailsClosed(
        int? registrationModeId,
        EventLocationRegistrationEffectiveState expectedState,
        bool expectedCoverage)
    {
        var result = Resolve(approvalStatusId: null, registrationModeId: registrationModeId);

        await Assert.That(result.EffectiveState).IsEqualTo(expectedState);
        await Assert.That(result.CoversRequestedEventLocation).IsEqualTo(expectedCoverage);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)ApprovalStatusEnum.Pending, EventLocationRegistrationEffectiveState.Pending, true)]
    [Arguments((int)ApprovalStatusEnum.Waitlisted, EventLocationRegistrationEffectiveState.Waitlisted, true)]
    [Arguments((int)ApprovalStatusEnum.Approved, EventLocationRegistrationEffectiveState.Confirmed, true)]
    [Arguments((int)ApprovalStatusEnum.Cancelled, EventLocationRegistrationEffectiveState.Cancelled, false)]
    [Arguments((int)ApprovalStatusEnum.Revoked, EventLocationRegistrationEffectiveState.Revoked, false)]
    [Arguments((int)ApprovalStatusEnum.Rejected, EventLocationRegistrationEffectiveState.Rejected, false)]
    [Arguments(999, EventLocationRegistrationEffectiveState.Denied, false)]
    public async Task Resolve_PersistedChildState_MapsToEffectiveAccess(
        int approvalStatusId,
        EventLocationRegistrationEffectiveState expectedState,
        bool expectedCoverage)
    {
        var result = Resolve(approvalStatusId: approvalStatusId);

        await Assert.That(result.EffectiveState).IsEqualTo(expectedState);
        await Assert.That(result.CoversRequestedEventLocation).IsEqualTo(expectedCoverage);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)ApprovalStatusEnum.Rejected, EventLocationRegistrationEffectiveState.Rejected)]
    [Arguments((int)ApprovalStatusEnum.Cancelled, EventLocationRegistrationEffectiveState.Cancelled)]
    [Arguments((int)ApprovalStatusEnum.Revoked, EventLocationRegistrationEffectiveState.Revoked)]
    [Arguments(999, EventLocationRegistrationEffectiveState.Denied)]
    public async Task Resolve_TerminalOrUnknownParent_DeniesLiveChild(
        int parentApprovalStatusId,
        EventLocationRegistrationEffectiveState expectedState)
    {
        var result = Resolve(parentApprovalStatusId: parentApprovalStatusId);

        await Assert.That(result.EffectiveState).IsEqualTo(expectedState);
        await Assert.That(result.CoveredEventSessionIds).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(true, false)]
    [Arguments(false, true)]
    public async Task Resolve_DeletedOrExpiredParent_DeniesLiveChild(bool isDeleted, bool isExpired)
    {
        var request = CreateRequest(
            intent: CreateIntent(
                isDeleted: isDeleted,
                expiresAtUtc: isExpired ? Now : Now.AddMinutes(1)));

        var result = _service.Resolve(request);

        await Assert.That(result.EffectiveState).IsEqualTo(EventLocationRegistrationEffectiveState.NonLive);
        await Assert.That(result.CoversRequestedEventLocation).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Resolve_CancelledChild_RemovesOnlyItsPlacementCoverage()
    {
        var cancelledSessionId = Guid.CreateVersion7();
        var survivingSessionId = Guid.CreateVersion7();
        var survivingLocationId = Guid.CreateVersion7();
        var request = CreateRequest(
            requestedEventLocationId: survivingLocationId,
            coverage:
            [
                CreateCoverage(
                    sessionId: cancelledSessionId,
                    eventLocationId: EventLocationId,
                    approvalStatusId: (int)ApprovalStatusEnum.Cancelled),
                CreateCoverage(
                    sessionId: survivingSessionId,
                    eventLocationId: survivingLocationId,
                    approvalStatusId: (int)ApprovalStatusEnum.Approved)
            ]);

        var result = _service.Resolve(request);

        await Assert.That(result.CoversRequestedEventLocation).IsTrue();
        await Assert.That(result.CoveredEventSessionIds).IsEquivalentTo([survivingSessionId]);
        await Assert.That(result.CoveredEventSessionIds).DoesNotContain(cancelledSessionId);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(RegistrationScopeEnum.Event, true, true)]
    [Arguments(RegistrationScopeEnum.Day, false, true)]
    [Arguments(RegistrationScopeEnum.SessionSelection, false, true)]
    public async Task Resolve_LiveScope_ReportsEventDayAndSessionCoverage(
        RegistrationScopeEnum scope,
        bool expectedWholeEvent,
        bool expectedLocationCoverage)
    {
        var intent = CreateIntent(scope: scope, selectedDayId: scope == RegistrationScopeEnum.Day ? DayId : null);
        var result = _service.Resolve(CreateRequest(intent: intent));

        await Assert.That(result.Scope).IsEqualTo(scope);
        await Assert.That(result.CoversWholeEvent).IsEqualTo(expectedWholeEvent);
        await Assert.That(result.CoveredEventDayId).IsEqualTo(scope == RegistrationScopeEnum.Day ? DayId : null);
        await Assert.That(result.CoveredEventSessionIds).IsEquivalentTo([SessionId]);
        await Assert.That(result.CoversRequestedEventLocation).IsEqualTo(expectedLocationCoverage);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Resolve_DayScope_IgnoresOtherDayAndOtherEventCoverage()
    {
        var otherSessionId = Guid.CreateVersion7();
        var request = CreateRequest(
            intent: CreateIntent(scope: RegistrationScopeEnum.Day, selectedDayId: DayId),
            coverage:
            [
                CreateCoverage(dayId: Guid.CreateVersion7(), eventLocationId: EventLocationId),
                CreateCoverage(eventId: Guid.CreateVersion7(), dayId: DayId, eventLocationId: EventLocationId),
                CreateCoverage(sessionId: otherSessionId, dayId: DayId, eventLocationId: Guid.CreateVersion7())
            ]);

        var result = _service.Resolve(request);

        await Assert.That(result.CoversRequestedEventLocation).IsFalse();
        await Assert.That(result.CoveredEventSessionIds).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationRegistrationEffectiveState.Pending, true, false)]
    [Arguments(EventLocationRegistrationEffectiveState.Waitlisted, true, false)]
    [Arguments(EventLocationRegistrationEffectiveState.Confirmed, true, true)]
    [Arguments(EventLocationRegistrationEffectiveState.Denied, false, false)]
    public async Task AudienceCeiling_OnlyConfirmedAllowsConfirmedParticipant(
        EventLocationRegistrationEffectiveState state,
        bool allowsCurrentRegistrant,
        bool allowsConfirmedParticipant)
    {
        var approvalStatusId = state switch
        {
            EventLocationRegistrationEffectiveState.Pending => (int)ApprovalStatusEnum.Pending,
            EventLocationRegistrationEffectiveState.Waitlisted => (int)ApprovalStatusEnum.Waitlisted,
            EventLocationRegistrationEffectiveState.Confirmed => (int)ApprovalStatusEnum.Approved,
            _ => (int)ApprovalStatusEnum.Cancelled
        };
        var result = Resolve(approvalStatusId: approvalStatusId);

        await Assert.That(result.AllowsAudience(LocationDisclosureAudienceEnum.Never)).IsFalse();
        await Assert.That(result.AllowsAudience(LocationDisclosureAudienceEnum.AnyCurrentRegistrant)).IsEqualTo(allowsCurrentRegistrant);
        await Assert.That(result.AllowsAudience(LocationDisclosureAudienceEnum.ConfirmedParticipant)).IsEqualTo(allowsConfirmedParticipant);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Resolve_ParentPending_CapsApprovedChildAtCurrentRegistrant()
    {
        var result = Resolve(
            approvalStatusId: (int)ApprovalStatusEnum.Approved,
            parentApprovalStatusId: (int)ApprovalStatusEnum.Pending);

        await Assert.That(result.EffectiveState).IsEqualTo(EventLocationRegistrationEffectiveState.Pending);
        await Assert.That(result.AllowsAudience(LocationDisclosureAudienceEnum.AnyCurrentRegistrant)).IsTrue();
        await Assert.That(result.AllowsAudience(LocationDisclosureAudienceEnum.ConfirmedParticipant)).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task Resolve_DeletedOrExpiredCoverage_DoesNotGrantRequestedPlacement()
    {
        var request = CreateRequest(
            coverage:
            [
                CreateCoverage(isDeleted: true),
                CreateCoverage(expiresAtUtc: Now)
            ]);

        var result = _service.Resolve(request);

        await Assert.That(result.EffectiveState).IsEqualTo(EventLocationRegistrationEffectiveState.NonLive);
        await Assert.That(result.CoversRequestedEventLocation).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ResolveMany_EventScope_CoversCurrentPlacementsIncludingLaterAddedCrossDaySessions()
    {
        var graph = CreateLoadedGraph(RegistrationScopeEnum.Event);
        var laterDayId = Guid.CreateVersion7();
        var laterLocationId = Guid.CreateVersion7();
        var laterSession = AddSession(graph, laterDayId, laterLocationId);

        var result = _service.ResolveMany(
            graph.TenantId,
            graph.Event.Id,
            graph.User.Id,
            Now,
            [graph.RegisteredLocationId, laterLocationId, laterLocationId],
            graph.Registrations);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[graph.RegisteredLocationId].CoversRequestedEventLocation).IsTrue();
        await Assert.That(result[laterLocationId].CoversRequestedEventLocation).IsTrue();
        await Assert.That(result[laterLocationId].CoversWholeEvent).IsTrue();
        await Assert.That(result[laterLocationId].CoveredEventSessionIds).Contains(laterSession.Id);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ResolveMany_DayScope_CoversOnlyCurrentPlacementsOnSelectedDay()
    {
        var graph = CreateLoadedGraph(RegistrationScopeEnum.Day);
        var laterLocationId = Guid.CreateVersion7();
        var otherDayLocationId = Guid.CreateVersion7();
        var laterSession = AddSession(graph, graph.DayId, laterLocationId);
        AddSession(graph, Guid.CreateVersion7(), otherDayLocationId);

        var result = _service.ResolveMany(
            graph.TenantId,
            graph.Event.Id,
            graph.User.Id,
            Now,
            [graph.RegisteredLocationId, laterLocationId, otherDayLocationId],
            graph.Registrations);

        await Assert.That(result[graph.RegisteredLocationId].CoversRequestedEventLocation).IsTrue();
        await Assert.That(result[laterLocationId].CoversRequestedEventLocation).IsTrue();
        await Assert.That(result[laterLocationId].CoveredEventSessionIds).Contains(laterSession.Id);
        await Assert.That(result[otherDayLocationId].CoversRequestedEventLocation).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ResolveMany_SessionSelection_DoesNotCoverUnselectedCurrentSession()
    {
        var graph = CreateLoadedGraph(RegistrationScopeEnum.SessionSelection);
        var unselectedLocationId = Guid.CreateVersion7();
        AddSession(graph, graph.DayId, unselectedLocationId);

        var result = _service.ResolveMany(
            graph.TenantId,
            graph.Event.Id,
            graph.User.Id,
            Now,
            [graph.RegisteredLocationId, unselectedLocationId],
            graph.Registrations);

        await Assert.That(result[graph.RegisteredLocationId].CoversRequestedEventLocation).IsTrue();
        await Assert.That(result[unselectedLocationId].CoversRequestedEventLocation).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ResolveMany_OverlappingIntents_SelectsStrongestValidAuthorityPerPlacement()
    {
        var broad = CreateLoadedGraph(
            RegistrationScopeEnum.Event,
            childApprovalStatusId: (int)ApprovalStatusEnum.Pending,
            parentApprovalStatusId: (int)ApprovalStatusEnum.Pending);
        var selected = AddRegistrationIntent(
            broad,
            RegistrationScopeEnum.SessionSelection,
            broad.RegisteredSession,
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Approved);
        var broadOnlyLocationId = Guid.CreateVersion7();
        AddSession(broad, broad.DayId, broadOnlyLocationId);

        var result = _service.ResolveMany(
            broad.TenantId,
            broad.Event.Id,
            broad.User.Id,
            Now,
            [broad.RegisteredLocationId, broadOnlyLocationId],
            [.. broad.Registrations, selected]);

        await Assert.That(result[broad.RegisteredLocationId].IntentId)
            .IsEqualTo(selected.EventRegistrationIntentId!.Value);
        await Assert.That(result[broad.RegisteredLocationId].EffectiveState)
            .IsEqualTo(EventLocationRegistrationEffectiveState.Confirmed);
        await Assert.That(result[broadOnlyLocationId].EffectiveState)
            .IsEqualTo(EventLocationRegistrationEffectiveState.Pending);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments("registration-tenant")]
    [Arguments("registration-event")]
    [Arguments("registration-user")]
    [Arguments("intent-tenant")]
    [Arguments("intent-event")]
    [Arguments("intent-user")]
    [Arguments("event-tenant")]
    [Arguments("session-tenant")]
    [Arguments("session-event")]
    [Arguments("placement-tenant")]
    [Arguments("placement-event")]
    [Arguments("placement-id")]
    [Arguments("parent-deleted")]
    [Arguments("child-deleted")]
    [Arguments("session-deleted")]
    [Arguments("placement-deleted")]
    public async Task ResolveMany_MalformedForeignOrDeletedLoadedGraph_ReturnsNoAuthority(string mutation)
    {
        var graph = CreateLoadedGraph(RegistrationScopeEnum.Event);
        var registration = graph.Registrations[0];
        var intent = registration.EventRegistrationIntent!;
        var session = registration.EventSession;
        var placement = session.EventLocation!;
        var foreignId = Guid.CreateVersion7();
        switch (mutation)
        {
            case "registration-tenant": registration.TenantId = foreignId; break;
            case "registration-event": registration.EventId = foreignId; break;
            case "registration-user": registration.UserId = foreignId; break;
            case "intent-tenant": intent.TenantId = foreignId; break;
            case "intent-event": intent.EventId = foreignId; break;
            case "intent-user": intent.UserId = foreignId; break;
            case "event-tenant": graph.Event.TenantId = foreignId; break;
            case "session-tenant": session.TenantId = foreignId; break;
            case "session-event": session.EventId = foreignId; break;
            case "placement-tenant":
                typeof(EventLocation).GetField("_tenantId", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .SetValue(placement, foreignId);
                break;
            case "placement-event": SetProperty(placement, nameof(EventLocation.EventId), foreignId); break;
            case "placement-id": SetProperty(placement, nameof(EventLocation.Id), foreignId); break;
            case "parent-deleted": intent.IsDeleted = true; break;
            case "child-deleted": registration.IsDeleted = true; break;
            case "session-deleted": session.IsDeleted = true; break;
            case "placement-deleted": placement.DetachFinalReference(graph.User.Id, Now.UtcDateTime); break;
        }

        var result = _service.ResolveMany(
            graph.TenantId,
            graph.Event.Id,
            graph.User.Id,
            Now,
            [graph.RegisteredLocationId],
            graph.Registrations);

        await Assert.That(!result.TryGetValue(graph.RegisteredLocationId, out var access)
            || !access.CoversRequestedEventLocation).IsTrue();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task EventRegistrationRepositoryContract_ReturnsEntitiesWithoutAuthorityOrQueryableLeak()
    {
        var offenders = typeof(IEventRegistrationRepository).GetMethods()
            .Select(method => $"{method.Name}:{method.ReturnType}")
            .Where(signature => signature.Contains(nameof(EventLocationRegistrationAccess), StringComparison.Ordinal)
                || signature.Contains(nameof(IQueryable), StringComparison.Ordinal))
            .ToArray();

        await Assert.That(offenders).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task ConstructionSurface_DoesNotExposePublicAuthorityConstructionOrMutation()
    {
        var type = typeof(EventLocationRegistrationAccess);
        var publicConstructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var publicFactories = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType == type)
            .ToArray();
        var writableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod is not null)
            .ToArray();

        await Assert.That(type.IsSealed).IsTrue();
        await Assert.That(publicConstructors).IsEmpty();
        await Assert.That(publicFactories).IsEmpty();
        await Assert.That(writableProperties).IsEmpty();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task NonPublicConstruction_DeniedWithCoverageDerivesNoAuthority()
    {
        var constructor = typeof(EventLocationRegistrationAccess)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();
        var access = (EventLocationRegistrationAccess)constructor.Invoke(
        [
            IntentId,
            RegistrationScopeEnum.Event,
            EventLocationRegistrationEffectiveState.Denied,
            EventId,
            true,
            DayId,
            ImmutableArray.Create(SessionId),
            EventLocationId,
            true
        ]);

        await Assert.That(access.CoversWholeEvent).IsFalse();
        await Assert.That(access.CoveredEventDayId).IsNull();
        await Assert.That(access.CoveredEventSessionIds).IsEmpty();
        await Assert.That(access.CoversRequestedEventLocation).IsFalse();
        await Assert.That(access.AudienceCeiling).IsEqualTo(LocationDisclosureAudienceEnum.Never);
        await Assert.That(access.AllowsAudience(LocationDisclosureAudienceEnum.ConfirmedParticipant)).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationRegistrationEffectiveState.Denied)]
    [Arguments(EventLocationRegistrationEffectiveState.Rejected)]
    [Arguments(EventLocationRegistrationEffectiveState.Cancelled)]
    [Arguments(EventLocationRegistrationEffectiveState.Revoked)]
    [Arguments(EventLocationRegistrationEffectiveState.NonLive)]
    public async Task AllowsAudience_MalformedTerminalAuthorityStillDenies(
        EventLocationRegistrationEffectiveState effectiveState)
    {
        var malformed = CreateMalformedAccess(
            effectiveState,
            coversRequestedEventLocation: true,
            LocationDisclosureAudienceEnum.ConfirmedParticipant);

        await Assert.That(malformed.AllowsAudience(LocationDisclosureAudienceEnum.AnyCurrentRegistrant)).IsFalse();
        await Assert.That(malformed.AllowsAudience(LocationDisclosureAudienceEnum.ConfirmedParticipant)).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments(EventLocationRegistrationEffectiveState.Pending, LocationDisclosureAudienceEnum.ConfirmedParticipant)]
    [Arguments(EventLocationRegistrationEffectiveState.Waitlisted, LocationDisclosureAudienceEnum.ConfirmedParticipant)]
    [Arguments(EventLocationRegistrationEffectiveState.Confirmed, LocationDisclosureAudienceEnum.AnyCurrentRegistrant)]
    [Arguments((EventLocationRegistrationEffectiveState)999, LocationDisclosureAudienceEnum.ConfirmedParticipant)]
    public async Task AllowsAudience_InvalidEffectiveStateAndCeilingCombinationDenies(
        EventLocationRegistrationEffectiveState effectiveState,
        LocationDisclosureAudienceEnum audienceCeiling)
    {
        var malformed = CreateMalformedAccess(effectiveState, true, audienceCeiling);

        await Assert.That(malformed.AllowsAudience(LocationDisclosureAudienceEnum.AnyCurrentRegistrant)).IsFalse();
        await Assert.That(malformed.AllowsAudience(LocationDisclosureAudienceEnum.ConfirmedParticipant)).IsFalse();
    }

    private EventLocationRegistrationAccess Resolve(
        int? approvalStatusId = (int)ApprovalStatusEnum.Approved,
        int? registrationModeId = (int)RegistrationModeEnum.Open,
        int? parentApprovalStatusId = null)
        => _service.Resolve(CreateRequest(
            intent: CreateIntent(approvalStatusId: parentApprovalStatusId),
            coverage:
            [
                CreateCoverage(
                    approvalStatusId: approvalStatusId,
                    registrationModeId: registrationModeId)
            ]));

    private static EventLocationRegistrationAccessRequest CreateRequest(
        Guid? requestedEventLocationId = null,
        EventLocationRegistrationIntentFact? intent = null,
        IReadOnlyCollection<EventLocationRegistrationCoverageFact>? coverage = null)
        => new(
            requestedEventLocationId ?? EventLocationId,
            Now,
            intent ?? CreateIntent(),
            (coverage ?? [CreateCoverage()]).ToImmutableArray());

    private static EventLocationRegistrationIntentFact CreateIntent(
        RegistrationScopeEnum scope = RegistrationScopeEnum.Event,
        Guid? selectedDayId = null,
        int? approvalStatusId = null,
        bool isDeleted = false,
        DateTimeOffset? expiresAtUtc = null)
        => new(
            IntentId,
            EventId,
            scope,
            selectedDayId,
            approvalStatusId,
            isDeleted,
            expiresAtUtc);

    private static EventLocationRegistrationCoverageFact CreateCoverage(
        Guid? intentId = null,
        Guid? eventId = null,
        Guid? dayId = null,
        Guid? sessionId = null,
        Guid? eventLocationId = null,
        int? approvalStatusId = (int)ApprovalStatusEnum.Approved,
        int? registrationModeId = (int)RegistrationModeEnum.Open,
        bool isDeleted = false,
        DateTimeOffset? expiresAtUtc = null)
        => new(
            intentId ?? IntentId,
            eventId ?? EventId,
            dayId ?? DayId,
            sessionId ?? SessionId,
            eventLocationId ?? EventLocationId,
            approvalStatusId,
            registrationModeId,
            isDeleted,
            expiresAtUtc);

    private static EventLocationRegistrationAccess CreateMalformedAccess(
        EventLocationRegistrationEffectiveState effectiveState,
        bool coversRequestedEventLocation,
        LocationDisclosureAudienceEnum audienceCeiling)
    {
        var access = (EventLocationRegistrationAccess)RuntimeHelpers.GetUninitializedObject(
            typeof(EventLocationRegistrationAccess));
        SetBackingField(access, nameof(EventLocationRegistrationAccess.EffectiveState), effectiveState);
        SetBackingField(access, nameof(EventLocationRegistrationAccess.CoversRequestedEventLocation), coversRequestedEventLocation);
        SetBackingField(access, nameof(EventLocationRegistrationAccess.AudienceCeiling), audienceCeiling);
        return access;
    }

    private static LoadedGraph CreateLoadedGraph(
        RegistrationScopeEnum scope,
        int? childApprovalStatusId = (int)ApprovalStatusEnum.Approved,
        int? parentApprovalStatusId = (int)ApprovalStatusEnum.Approved)
    {
        var tenantId = Guid.CreateVersion7();
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii { Email = "access@example.test", FirstName = "Access", LastName = "Test" }
        };
        var @event = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Access test",
            Actor = null!,
            TenantId = tenantId,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
        var dayId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();
        var graph = new LoadedGraph(tenantId, user, @event, dayId, locationId);
        var session = AddSession(graph, dayId, locationId);
        graph.RegisteredSession = session;
        graph.Registrations.Add(AddRegistrationIntent(
            graph,
            scope,
            session,
            childApprovalStatusId,
            parentApprovalStatusId));
        return graph;
    }

    private static EventSession AddSession(LoadedGraph graph, Guid dayId, Guid eventLocationId)
    {
        var eventLocation = EventLocation.CreatePhysical(
            graph.TenantId,
            graph.Event.Id,
            Guid.CreateVersion7(),
            graph.User.Id,
            Now.UtcDateTime);
        SetProperty(eventLocation, nameof(EventLocation.Id), eventLocationId);
        var session = new EventSession
        {
            Id = Guid.CreateVersion7(),
            EventId = graph.Event.Id,
            Event = graph.Event,
            EventDayId = dayId,
            TenantId = graph.TenantId,
            Tenant = null!,
            RegistrationModeId = (int)RegistrationModeEnum.Open
        };
        session.AssignEventLocation(eventLocation);
        graph.Event.Sessions.Add(session);
        return session;
    }

    private static EventRegistration AddRegistrationIntent(
        LoadedGraph graph,
        RegistrationScopeEnum scope,
        EventSession session,
        int? childApprovalStatusId,
        int? parentApprovalStatusId)
    {
        var intent = new EventRegistrationIntent
        {
            Id = Guid.CreateVersion7(),
            EventId = graph.Event.Id,
            Event = graph.Event,
            UserId = graph.User.Id,
            User = graph.User,
            RegistrationScopeId = (int)scope,
            RegistrationScope = null!,
            SelectedEventDayId = scope == RegistrationScopeEnum.Day ? graph.DayId : null,
            ApprovalStatusId = parentApprovalStatusId,
            TenantId = graph.TenantId,
            Tenant = null!
        };
        return new EventRegistration
        {
            Id = Guid.CreateVersion7(),
            EventId = graph.Event.Id,
            Event = graph.Event,
            UserId = graph.User.Id,
            User = graph.User,
            EventSessionId = session.Id,
            EventSession = session,
            EventRegistrationIntentId = intent.Id,
            EventRegistrationIntent = intent,
            ApprovalStatusId = childApprovalStatusId,
            TenantId = graph.TenantId,
            Tenant = null!
        };
    }

    private sealed class LoadedGraph(
        Guid tenantId,
        User user,
        Explore.Domain.Event @event,
        Guid dayId,
        Guid registeredLocationId)
    {
        public Guid TenantId { get; } = tenantId;
        public User User { get; } = user;
        public Explore.Domain.Event Event { get; } = @event;
        public Guid DayId { get; } = dayId;
        public Guid RegisteredLocationId { get; } = registeredLocationId;
        public EventSession RegisteredSession { get; set; } = null!;
        public List<EventRegistration> Registrations { get; } = [];
    }

    private static void SetBackingField<T>(EventLocationRegistrationAccess access, string propertyName, T value)
        => typeof(EventLocationRegistrationAccess)
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(access, value);

    private static void SetProperty<T>(EventLocation eventLocation, string propertyName, T value)
        => typeof(EventLocation)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(eventLocation, value);
}
