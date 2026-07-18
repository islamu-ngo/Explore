// ABOUTME: Unit tests for policy-aware event and session lifecycle readiness evaluation.
// ABOUTME: Verifies machine-readable field errors, session profiles, and transition hard invariants.

using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public sealed class EventLifecycleReadinessEvaluatorTests
{
    private readonly EventLifecycleReadinessEvaluator _evaluator = new();

    [Test]
    public async Task EvaluateEventPublishWhenScheduleIsMissingReturnsMachineReadableErrors()
    {
        var eventEntity = CreateReadyEvent();
        eventEntity.FirstSessionStartUtc = null;

        var result = _evaluator.Evaluate(eventEntity, ValidationProfile.EventPublish, CreateEventPublishPolicy());

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).Contains("schedule_session_required");
        await Assert.That(result.Errors.Select(error => error.Code)).Contains("schedule_first_start_required");
        await Assert.That(result.Errors.Single(error => error.Code == "schedule_session_required").FieldPath).IsEqualTo("schedule.sessions");
        await Assert.That(result.Errors.All(error => error.Profile == ValidationProfile.EventPublish)).IsTrue();
    }

    [Test]
    public async Task EvaluateCommunityPublishWhenEventIsModeratedReturnsHardInvariantError()
    {
        var eventEntity = CreateReadyEvent();
        eventEntity.EventStatusId = (int)EventStatusEnum.Moderated;

        LifecycleReadinessResult result = _evaluator.Evaluate(
            eventEntity,
            ValidationProfile.EventPublishCommunityLexicon,
            CreateCommunityPublishPolicy());

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).Contains("event_moderated");
        await Assert.That(result.Errors.Single(error => error.Code == "event_moderated").Source)
            .IsEqualTo(ReadinessErrorSource.HardInvariant);
    }

    [Test]
    public async Task EvaluateSessionDraftCreateWhenMinimalDraftFieldsExistReturnsReady()
    {
        var session = CreateDraftSession();

        var result = _evaluator.Evaluate(session, parentEvent: null, ValidationProfile.SessionDraftCreate, CreateSessionDraftPolicy());

        await Assert.That(result.IsReady).IsTrue();
        await Assert.That(result.Errors).IsEmpty();
    }

    [Test]
    public async Task EvaluateSessionScheduleWhenEndIsBeforeStartReturnsRangeError()
    {
        var session = CreateDraftSession();
        session.StartTime = DateTimeOffset.UtcNow.AddHours(2);
        session.EndTime = DateTimeOffset.UtcNow.AddHours(1);

        var result = _evaluator.Evaluate(session, parentEvent: null, ValidationProfile.SessionSchedule, CreateSessionSchedulePolicy());

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).Contains("session_schedule_range_invalid");
        await Assert.That(result.Errors.Single(error => error.Code == "session_schedule_range_invalid").Source).IsEqualTo(ReadinessErrorSource.DomainRule);
    }

    [Test]
    public async Task EvaluateSessionPublishWhenParentEventIsDraftReturnsCompatibilityError()
    {
        var session = CreateDraftSession();
        session.StartTime = DateTimeOffset.UtcNow.AddDays(1);
        session.EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1);
        var parentEvent = CreateReadyEvent();
        parentEvent.EventStatusId = (int)EventStatusEnum.Draft;

        var result = _evaluator.Evaluate(session, parentEvent, ValidationProfile.SessionPublish, CreateSessionPublishPolicy());

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).Contains("session_parent_event_not_published");
        await Assert.That(result.Errors.Single(error => error.Code == "session_parent_event_not_published").FieldPath).IsEqualTo("event.status");
    }

    [Test]
    public async Task EvaluateSessionPublishWhenSessionIsRejectedReturnsHardInvariantError()
    {
        var session = CreateDraftSession();
        session.EventSessionStatusId = (int)EventSessionStatusEnum.Rejected;
        session.StartTime = DateTimeOffset.UtcNow.AddDays(1);
        session.EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(1);
        var parentEvent = CreateReadyEvent();
        parentEvent.EventStatusId = (int)EventStatusEnum.Published;

        var result = _evaluator.Evaluate(session, parentEvent, ValidationProfile.SessionPublish, CreateSessionPublishPolicy());

        await Assert.That(result.IsReady).IsFalse();
        await Assert.That(result.Errors.Select(error => error.Code)).Contains("session_rejected");
        await Assert.That(result.Errors.Single(error => error.Code == "session_rejected").Source).IsEqualTo(ReadinessErrorSource.HardInvariant);
    }

    private static EventLifecyclePolicy CreateEventPublishPolicy() => new()
    {
        Profile = ValidationProfile.EventPublish,
        RequiredEventFields = new HashSet<Enum>
        {
            EventFieldKey.Title,
            EventFieldKey.Tenant,
            EventFieldKey.Owner,
            EventFieldKey.Status,
            EventFieldKey.Visibility,
            EventFieldKey.Format,
            EventFieldKey.ScheduleSessions,
            EventFieldKey.ScheduleFirstStart
        },
        RequiredSessionFields = new HashSet<Enum>()
    };

    private static EventLifecyclePolicy CreateCommunityPublishPolicy() => new()
    {
        Profile = ValidationProfile.EventPublishCommunityLexicon,
        RequiredEventFields = new HashSet<Enum>
        {
            EventFieldKey.Title,
            EventFieldKey.Tenant,
            EventFieldKey.Owner,
            EventFieldKey.Status
        },
        RequiredSessionFields = new HashSet<Enum>()
    };

    private static EventLifecyclePolicy CreateSessionDraftPolicy() => new()
    {
        Profile = ValidationProfile.SessionDraftCreate,
        RequiredEventFields = new HashSet<Enum>(),
        RequiredSessionFields = new HashSet<Enum>
        {
            EventSessionFieldKey.ParentEvent,
            EventSessionFieldKey.Tenant,
            EventSessionFieldKey.Status,
            EventSessionFieldKey.Title
        }
    };

    private static EventLifecyclePolicy CreateSessionSchedulePolicy() => new()
    {
        Profile = ValidationProfile.SessionSchedule,
        RequiredEventFields = new HashSet<Enum>(),
        RequiredSessionFields = new HashSet<Enum>
        {
            EventSessionFieldKey.ParentEvent,
            EventSessionFieldKey.Tenant,
            EventSessionFieldKey.Status,
            EventSessionFieldKey.ScheduleStart,
            EventSessionFieldKey.ScheduleEnd
        }
    };

    private static EventLifecyclePolicy CreateSessionPublishPolicy() => new()
    {
        Profile = ValidationProfile.SessionPublish,
        RequiredEventFields = new HashSet<Enum>(),
        RequiredSessionFields = new HashSet<Enum>
        {
            EventSessionFieldKey.ParentEvent,
            EventSessionFieldKey.Tenant,
            EventSessionFieldKey.Status,
            EventSessionFieldKey.Title,
            EventSessionFieldKey.ScheduleStart,
            EventSessionFieldKey.ScheduleEnd,
            EventSessionFieldKey.ParentEventCompatibility
        }
    };

    private static Explore.Domain.Event CreateReadyEvent() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Ready Event",
        ActorId = Guid.NewGuid(),
        Actor = null!,
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        VisibilityTypeId = 1,
        VisibilityType = null!,
        EventStatusId = (int)EventStatusEnum.Draft,
        EventStatus = null!,
        EventFormatId = 1,
        EventFormat = null!,
        FirstSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1),
        LastSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(1)
    };

    private static EventSession CreateDraftSession() => new()
    {
        Id = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        Event = null!,
        TenantId = Guid.NewGuid(),
        Tenant = null!,
        Title = "Draft session",
        EventSessionStatusId = (int)EventSessionStatusEnum.Draft
    };
}
