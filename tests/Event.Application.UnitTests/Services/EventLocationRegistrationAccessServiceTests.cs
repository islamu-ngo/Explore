// ABOUTME: Exhaustive registration-state and scope tests for EventLocation attendee access.
// ABOUTME: Proves fail-closed lifecycle handling, null-approval modes, audience ceilings, and repository separation.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services;
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
    public async Task PersistenceRepositoryContracts_DoNotReturnRegistrationAccessDto()
    {
        var offenders = typeof(IEventRegistrationRepository).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == "Explore.Application.Contracts.Persistence")
            .SelectMany(type => type.GetMethods().Select(method => $"{type.Name}.{method.Name}:{method.ReturnType}"))
            .Where(signature => signature.Contains(nameof(EventLocationRegistrationAccess), StringComparison.Ordinal))
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

    private static void SetBackingField<T>(EventLocationRegistrationAccess access, string propertyName, T value)
        => typeof(EventLocationRegistrationAccess)
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(access, value);
}
