// ABOUTME: Source coverage for fanout occurrence precedence, coalescing, replay, and pointer creation.
// ABOUTME: Proves closed policy ordering, scope isolation, immutable merge rules, and fail-closed transitions.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Notifications;

public sealed class NotificationFanoutOccurrenceCoordinatorTests
{
    [Test]
    public async Task CoordinatorAcquiresSourceThenEventLocksBeforeAnyCoordinationRead()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            sessionId: null);

        await fixture.Coordinator.CoordinateInCurrentTransactionAsync(candidate);

        Received.InOrder(() =>
        {
            fixture.OccurrenceRepository.AcquireSourceThenEventCoordinationLocksAsync(
                candidate.TenantId,
                candidate.SourceType,
                candidate.SourceId,
                candidate.AggregateVersion,
                candidate.EventId,
                Arg.Any<CancellationToken>());
            fixture.OccurrenceRepository.GetBySourceIdentityForCoordinationAsync(
                candidate.TenantId,
                candidate.SourceType,
                candidate.SourceId,
                candidate.AggregateVersion,
                Arg.Any<CancellationToken>());
            fixture.OccurrenceRepository.GetPendingForEventCoordinationAsync(
                candidate.TenantId,
                candidate.EventId,
                Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task SessionCandidateFromAnotherEventFailsBeforeReplayOrCreation()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            fixture.SessionA);
        fixture.OccurrenceRepository.SessionBelongsToEventForCoordinationAsync(
                candidate.TenantId,
                candidate.EventId,
                candidate.SessionId!.Value,
                Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(candidate));

        await fixture.OccurrenceRepository.Received(1).AcquireSourceThenEventCoordinationLocksAsync(
            candidate.TenantId,
            candidate.SourceType,
            candidate.SourceId,
            candidate.AggregateVersion,
            candidate.EventId,
            Arg.Any<CancellationToken>());
        await fixture.OccurrenceRepository.DidNotReceiveWithAnyArgs()
            .GetBySourceIdentityForCoordinationAsync(default, default!, default, default, default);
        await fixture.OccurrenceRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    [Arguments(NotificationFanoutOccurrenceKind.HeavyModerationUnavailable, NotificationFanoutOccurrenceKind.EventCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.EventCancellation, NotificationFanoutOccurrenceKind.HeavyModerationUnavailable)]
    [Arguments(NotificationFanoutOccurrenceKind.HeavyModerationUnavailable, NotificationFanoutOccurrenceKind.SessionCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.SessionCancellation, NotificationFanoutOccurrenceKind.HeavyModerationUnavailable)]
    [Arguments(NotificationFanoutOccurrenceKind.HeavyModerationUnavailable, NotificationFanoutOccurrenceKind.ImportantUpdate)]
    [Arguments(NotificationFanoutOccurrenceKind.ImportantUpdate, NotificationFanoutOccurrenceKind.HeavyModerationUnavailable)]
    [Arguments(NotificationFanoutOccurrenceKind.HeavyModerationUnavailable, NotificationFanoutOccurrenceKind.Reminder)]
    [Arguments(NotificationFanoutOccurrenceKind.Reminder, NotificationFanoutOccurrenceKind.HeavyModerationUnavailable)]
    [Arguments(NotificationFanoutOccurrenceKind.EventCancellation, NotificationFanoutOccurrenceKind.SessionCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.SessionCancellation, NotificationFanoutOccurrenceKind.EventCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.EventCancellation, NotificationFanoutOccurrenceKind.ImportantUpdate)]
    [Arguments(NotificationFanoutOccurrenceKind.ImportantUpdate, NotificationFanoutOccurrenceKind.EventCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.EventCancellation, NotificationFanoutOccurrenceKind.Reminder)]
    [Arguments(NotificationFanoutOccurrenceKind.Reminder, NotificationFanoutOccurrenceKind.EventCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.SessionCancellation, NotificationFanoutOccurrenceKind.ImportantUpdate)]
    [Arguments(NotificationFanoutOccurrenceKind.ImportantUpdate, NotificationFanoutOccurrenceKind.SessionCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.SessionCancellation, NotificationFanoutOccurrenceKind.Reminder)]
    [Arguments(NotificationFanoutOccurrenceKind.Reminder, NotificationFanoutOccurrenceKind.SessionCancellation)]
    [Arguments(NotificationFanoutOccurrenceKind.ImportantUpdate, NotificationFanoutOccurrenceKind.Reminder)]
    [Arguments(NotificationFanoutOccurrenceKind.Reminder, NotificationFanoutOccurrenceKind.ImportantUpdate)]
    public async Task EveryPrecedencePairKeepsOnlyTheHigherActiveKind(
        NotificationFanoutOccurrenceKind existingKind,
        NotificationFanoutOccurrenceKind incomingKind)
    {
        var fixture = new Fixture();
        bool needsSession = existingKind == NotificationFanoutOccurrenceKind.SessionCancellation
            || incomingKind == NotificationFanoutOccurrenceKind.SessionCancellation;
        Guid? sessionId = needsSession ? fixture.SessionA : null;
        NotificationFanoutOccurrence existing = fixture.Persisted(existingKind, fixture.At, sessionId);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(incomingKind, fixture.At.AddMinutes(1), sessionId);
        fixture.Pending(existing);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);

        bool incomingWins = Priority(incomingKind) > Priority(existingKind);
        await Assert.That(result.Outcome).IsEqualTo(incomingWins
            ? NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive
            : NotificationFanoutOccurrenceCoordinationOutcome.Superseded);
        await Assert.That(result.ActiveOccurrenceId).IsEqualTo(incomingWins ? incoming.OccurrenceId : existing.Id);
        await Assert.That(fixture.OutboxPointers).Count().IsEqualTo(incomingWins ? 1 : 0);
        if (incomingWins)
        {
            await Assert.That(existing.State).IsEqualTo(NotificationFanoutOccurrenceState.Superseded);
            await Assert.That(fixture.OutboxPointers[0].Id).IsEqualTo(incoming.PointerOutboxMessageId);
        }
    }

    [Test]
    public async Task SessionCancellationDoesNotCompeteWithAnotherSession()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence existing = fixture.Persisted(
            NotificationFanoutOccurrenceKind.SessionCancellation,
            fixture.At,
            fixture.SessionA);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            fixture.SessionB);
        fixture.Pending(existing);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive);
        await Assert.That(existing.State).IsEqualTo(NotificationFanoutOccurrenceState.Pending);
        await fixture.OccurrenceRepository.DidNotReceiveWithAnyArgs()
            .TryPersistSupersessionAsync(default!, default);
    }

    [Test]
    public async Task InWindowUpdateUsesEarliestBeforeLatestAfterLatestCutoffAndSlidingQuietWindow()
    {
        var fixture = new Fixture();
        string earliestBefore = fixture.Snapshot("Initial", fixture.At);
        string intermediate = fixture.Snapshot("Intermediate", fixture.At.AddHours(1));
        string latestAfter = fixture.Snapshot("Latest", fixture.At.AddHours(2));
        NotificationFanoutOccurrence existing = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            sessionId: null,
            NotificationFanoutChangeField.StartTime,
            earliestBefore,
            intermediate);
        DateTime latestOccurredAt = fixture.At.AddMinutes(4);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            latestOccurredAt,
            sessionId: null,
            NotificationFanoutChangeField.EndTime,
            intermediate,
            latestAfter) with
        {
            AudienceCutoffAt = latestOccurredAt.AddSeconds(-1)
        };
        fixture.Pending(existing);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(result.Occurrence);

        await Assert.That(result.Occurrence.SafeBeforeSnapshotJson).IsEqualTo(earliestBefore);
        await Assert.That(result.Occurrence.SafeAfterSnapshotJson).IsEqualTo(latestAfter);
        await Assert.That(result.Occurrence.AudienceCutoffAt).IsEqualTo(incoming.AudienceCutoffAt);
        await Assert.That(template.ChangeSet.Fields).IsEquivalentTo([
            NotificationFanoutChangeField.StartTime,
            NotificationFanoutChangeField.EndTime]);
        await Assert.That(result.Occurrence.NotBefore).IsEqualTo(latestOccurredAt.AddMinutes(5));
        await Assert.That(result.Occurrence.CoalescingWindowEndsAt).IsEqualTo(latestOccurredAt.AddMinutes(5));
    }

    [Test]
    public async Task UpdateOutsideWindowStartsNewWindowWithoutMergingBeforeOrFields()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence existing = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            sessionId: null,
            NotificationFanoutChangeField.StartTime);
        DateTime occurredAt = fixture.At.AddMinutes(6);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            occurredAt,
            sessionId: null,
            NotificationFanoutChangeField.EndTime);
        fixture.Pending(existing);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(result.Occurrence);

        await Assert.That(result.Occurrence.SafeBeforeSnapshotJson).IsEqualTo(incoming.SafeBeforeSnapshotJson);
        await Assert.That(template.ChangeSet.Fields).IsEquivalentTo([NotificationFanoutChangeField.EndTime]);
        await Assert.That(result.Occurrence.NotBefore).IsEqualTo(occurredAt.AddMinutes(5));
    }

    [Test]
    public async Task OlderEqualUpdateCannotReplaceNewerPendingUpdate()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence existing = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(2),
            sessionId: null);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null);
        fixture.Pending(existing);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.Superseded);
        await Assert.That(result.ActiveOccurrenceId).IsEqualTo(existing.Id);
        await Assert.That(fixture.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task ExactSourceReplayResolvesTheCurrentActiveWinnerWithoutCreatingPointer()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.Reminder,
            fixture.At,
            sessionId: null);
        NotificationFanoutOccurrence replay = fixture.Persisted(candidate);
        NotificationFanoutOccurrence middle = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null);
        NotificationFanoutOccurrence active = fixture.Persisted(
            NotificationFanoutOccurrenceKind.EventCancellation,
            fixture.At.AddMinutes(2),
            sessionId: null);
        replay.Supersede(middle.Id, "superseded_by_newer_update", fixture.At.AddMinutes(1));
        middle.Supersede(active.Id, "superseded_by_event_cancellation", fixture.At.AddMinutes(2));
        fixture.Replay(replay, middle, active);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(candidate);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.SourceReplay);
        await Assert.That(result.ActiveOccurrenceId).IsEqualTo(active.Id);
        await Assert.That(fixture.CreatedOccurrences).IsEmpty();
        await Assert.That(fixture.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task SourceReplayRejectsCrossSessionSupersessionHop()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.Reminder,
            fixture.At,
            fixture.SessionA);
        NotificationFanoutOccurrence replay = fixture.Persisted(candidate);
        NotificationFanoutOccurrence invalidReplacement = fixture.Persisted(
            NotificationFanoutOccurrenceKind.Reminder,
            fixture.At.AddMinutes(1),
            fixture.SessionB);
        replay.Supersede(invalidReplacement.Id, "duplicate_reminder", fixture.At.AddMinutes(1));
        fixture.Replay(replay, invalidReplacement);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(candidate));
    }

    [Test]
    public async Task SourceReplayRejectsLowerPriorityReplacementHop()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.EventCancellation,
            fixture.At,
            sessionId: null);
        NotificationFanoutOccurrence replay = fixture.Persisted(candidate);
        NotificationFanoutOccurrence invalidReplacement = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null);
        replay.Supersede(invalidReplacement.Id, "invalid_lower_priority", fixture.At.AddMinutes(1));
        fixture.Replay(replay, invalidReplacement);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(candidate));
    }

    [Test]
    public async Task SourceReplayRejectsReverseOrderedEqualUpdateHop()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(2),
            sessionId: null);
        NotificationFanoutOccurrence replay = fixture.Persisted(candidate);
        NotificationFanoutOccurrence invalidReplacement = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null);
        replay.Supersede(invalidReplacement.Id, "superseded_by_newer_update", fixture.At.AddMinutes(2));
        fixture.Replay(replay, invalidReplacement);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(candidate));
    }

    [Test]
    public async Task SourceIdentityWithDifferentEventOrPayloadFailsClosed()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.EventCancellation,
            fixture.At,
            sessionId: null);
        NotificationFanoutOccurrence replay = fixture.Persisted(candidate with
        {
            EventId = Guid.CreateVersion7(),
            SafeAfterSnapshotJson = fixture.Snapshot("Conflicting", fixture.At)
        });
        fixture.Replay(replay);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(candidate));
        await Assert.That(fixture.CreatedOccurrences).IsEmpty();
        await Assert.That(fixture.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task ConditionalSupersessionMissThrowsBeforePointerCreation()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence existing = fixture.Persisted(
            NotificationFanoutOccurrenceKind.Reminder,
            fixture.At,
            sessionId: null);
        fixture.Pending(existing);
        fixture.OccurrenceRepository.TryPersistSupersessionAsync(
                Arg.Any<NotificationFanoutOccurrence>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(fixture.Candidate(
                NotificationFanoutOccurrenceKind.ImportantUpdate,
                fixture.At.AddMinutes(1),
                sessionId: null)));
        await Assert.That(fixture.OutboxPointers).IsEmpty();
        await fixture.EmailSuppressionRepository.DidNotReceiveWithAnyArgs()
            .SuppressPreHandoffAsync(default, default, default, default);
    }

    [Test]
    public async Task SuccessfulSessionSupersessionSuppressesOnlyTheTransitionedOccurrence()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence sameSession = fixture.Persisted(
            NotificationFanoutOccurrenceKind.Reminder,
            fixture.At,
            fixture.SessionA);
        NotificationFanoutOccurrence otherSession = fixture.Persisted(
            NotificationFanoutOccurrenceKind.Reminder,
            fixture.At,
            fixture.SessionB);
        fixture.Pending(sameSession, otherSession);
        NotificationFanoutOccurrenceCandidate candidate = fixture.Candidate(
            NotificationFanoutOccurrenceKind.SessionCancellation,
            fixture.At.AddMinutes(1),
            fixture.SessionA);

        await fixture.Coordinator.CoordinateInCurrentTransactionAsync(candidate);

        await fixture.EmailSuppressionRepository.Received(1).SuppressPreHandoffAsync(
            sameSession.TenantId,
            sameSession.Id,
            candidate.OccurredAt,
            Arg.Any<CancellationToken>());
        await fixture.EmailSuppressionRepository.DidNotReceive().SuppressPreHandoffAsync(
            otherSession.TenantId,
            otherSession.Id,
            candidate.OccurredAt,
            Arg.Any<CancellationToken>());
    }

    private static int Priority(NotificationFanoutOccurrenceKind kind) => kind switch
    {
        NotificationFanoutOccurrenceKind.Reminder => NotificationFanoutOccurrenceCoordinationPolicy.ReminderPriority,
        NotificationFanoutOccurrenceKind.ImportantUpdate => NotificationFanoutOccurrenceCoordinationPolicy.ImportantUpdatePriority,
        NotificationFanoutOccurrenceKind.SessionCancellation => NotificationFanoutOccurrenceCoordinationPolicy.SessionCancellationPriority,
        NotificationFanoutOccurrenceKind.EventCancellation => NotificationFanoutOccurrenceCoordinationPolicy.EventCancellationPriority,
        NotificationFanoutOccurrenceKind.HeavyModerationUnavailable => NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailablePriority,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed class Fixture
    {
        public Fixture()
        {
            OccurrenceRepository.AcquireSourceThenEventCoordinationLocksAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            OccurrenceRepository.GetBySourceIdentityForCoordinationAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns((NotificationFanoutOccurrence?)null);
            OccurrenceRepository.GetPendingForEventCoordinationAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(Array.Empty<NotificationFanoutOccurrence>());
            OccurrenceRepository.SessionBelongsToEventForCoordinationAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            OccurrenceRepository.TryPersistSupersessionAsync(
                    Arg.Any<NotificationFanoutOccurrence>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
            EmailSuppressionRepository.SuppressPreHandoffAsync(
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(new NotificationFanoutEmailSuppressionResult(0, 0));
            OccurrenceRepository.Create(Arg.Any<NotificationFanoutOccurrence>())
                .Returns(call =>
                {
                    NotificationFanoutOccurrence occurrence = call.Arg<NotificationFanoutOccurrence>();
                    CreatedOccurrences.Add(occurrence);
                    return occurrence;
                });
            OutboxRepository.Create(Arg.Any<OutboxMessage>())
                .Returns(call =>
                {
                    OutboxMessage message = call.Arg<OutboxMessage>();
                    OutboxPointers.Add(message);
                    return message;
                });
            Coordinator = new(
                OccurrenceRepository,
                EmailSuppressionRepository,
                OutboxRepository,
                new NotificationFanoutRecipientTemplateFactory());
        }

        public DateTime At { get; } = new(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);
        public Guid TenantId { get; } = Guid.CreateVersion7();
        public Guid EventId { get; } = Guid.CreateVersion7();
        public Guid SessionA { get; } = Guid.CreateVersion7();
        public Guid SessionB { get; } = Guid.CreateVersion7();
        public INotificationFanoutOccurrenceRepository OccurrenceRepository { get; } =
            Substitute.For<INotificationFanoutOccurrenceRepository>();
        public INotificationFanoutEmailSuppressionRepository EmailSuppressionRepository { get; } =
            Substitute.For<INotificationFanoutEmailSuppressionRepository>();
        public IOutboxRepository OutboxRepository { get; } = Substitute.For<IOutboxRepository>();
        public NotificationFanoutOccurrenceCoordinator Coordinator { get; }
        public List<NotificationFanoutOccurrence> CreatedOccurrences { get; } = [];
        public List<OutboxMessage> OutboxPointers { get; } = [];

        public void Pending(params NotificationFanoutOccurrence[] occurrences)
        {
            OccurrenceRepository.GetPendingForEventCoordinationAsync(
                    TenantId,
                    EventId,
                    Arg.Any<CancellationToken>())
                .Returns(occurrences);
        }

        public void Replay(
            NotificationFanoutOccurrence replay,
            params NotificationFanoutOccurrence[] replacements)
        {
            OccurrenceRepository.GetBySourceIdentityForCoordinationAsync(
                    TenantId,
                    replay.SourceType,
                    replay.SourceId,
                    replay.AggregateVersion,
                    Arg.Any<CancellationToken>())
                .Returns(replay);
            OccurrenceRepository.GetByIdForCoordinationAsync(
                    TenantId,
                    Arg.Any<Guid>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => replacements.SingleOrDefault(value => value.Id == call.ArgAt<Guid>(1)));
        }

        public NotificationFanoutOccurrenceCandidate Candidate(
            NotificationFanoutOccurrenceKind kind,
            DateTime occurredAt,
            Guid? sessionId,
            NotificationFanoutChangeField changeField = NotificationFanoutChangeField.StartTime,
            string? beforeJson = null,
            string? afterJson = null)
        {
            bool cancellation = kind is NotificationFanoutOccurrenceKind.EventCancellation
                or NotificationFanoutOccurrenceKind.SessionCancellation;
            bool sessionScoped = kind == NotificationFanoutOccurrenceKind.SessionCancellation
                || sessionId.HasValue && kind is NotificationFanoutOccurrenceKind.ImportantUpdate
                    or NotificationFanoutOccurrenceKind.Reminder;
            Guid? effectiveSessionId = sessionScoped ? sessionId ?? SessionA : null;
            string templateKey = kind switch
            {
                NotificationFanoutOccurrenceKind.HeavyModerationUnavailable => NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailableTemplateKey,
                NotificationFanoutOccurrenceKind.EventCancellation => NotificationFanoutRecipientTemplateFactory.EventCancelledTemplateKey,
                NotificationFanoutOccurrenceKind.SessionCancellation => NotificationFanoutRecipientTemplateFactory.SessionCancelledTemplateKey,
                NotificationFanoutOccurrenceKind.ImportantUpdate when sessionScoped => NotificationFanoutRecipientTemplateFactory.SessionUpdatedTemplateKey,
                NotificationFanoutOccurrenceKind.ImportantUpdate => NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
                NotificationFanoutOccurrenceKind.Reminder when sessionScoped => NotificationFanoutOccurrenceCoordinationPolicy.SessionReminderTemplateKey,
                NotificationFanoutOccurrenceKind.Reminder => NotificationFanoutOccurrenceCoordinationPolicy.EventReminderTemplateKey,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            int deliveryPolicyId = kind switch
            {
                NotificationFanoutOccurrenceKind.HeavyModerationUnavailable => (int)NotificationDeliveryPolicyEnum.ModerationAvailabilityRequired,
                NotificationFanoutOccurrenceKind.Reminder => (int)NotificationDeliveryPolicyEnum.ReminderOptional,
                _ => (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional
            };
            string changeSet = kind is NotificationFanoutOccurrenceKind.HeavyModerationUnavailable
                or NotificationFanoutOccurrenceKind.Reminder
                    ? "{}"
                    : NotificationFanoutTemplateJson.Serialize(new NotificationFanoutChangeSetV1([
                        cancellation ? NotificationFanoutChangeField.Cancelled : changeField]));
            string before = beforeJson ?? Snapshot("Before", occurredAt, sessionScoped);
            string after = afterJson ?? Snapshot("After", occurredAt.AddHours(1), sessionScoped);

            return new(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                TenantId,
                EventId,
                effectiveSessionId,
                occurredAt,
                occurredAt,
                Guid.CreateVersion7(),
                changeSet,
                before,
                after,
                templateKey,
                NotificationFanoutRecipientTemplateFactory.CurrentTemplateVersion,
                deliveryPolicyId,
                NotificationFanoutRecipientTemplateFactory.CurrentPolicyVersion,
                kind == NotificationFanoutOccurrenceKind.Reminder ? occurredAt.AddHours(1) : occurredAt,
                "event-mutation",
                Guid.CreateVersion7());
        }

        public NotificationFanoutOccurrence Persisted(
            NotificationFanoutOccurrenceKind kind,
            DateTime occurredAt,
            Guid? sessionId,
            NotificationFanoutChangeField changeField = NotificationFanoutChangeField.StartTime,
            string? beforeJson = null,
            string? afterJson = null) => Persisted(Candidate(
                kind,
                occurredAt,
                sessionId,
                changeField,
                beforeJson,
                afterJson));

        public NotificationFanoutOccurrence Persisted(NotificationFanoutOccurrenceCandidate candidate)
        {
            NotificationFanoutOccurrenceKind kind = candidate.TemplateKey switch
            {
                NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailableTemplateKey => NotificationFanoutOccurrenceKind.HeavyModerationUnavailable,
                NotificationFanoutRecipientTemplateFactory.EventCancelledTemplateKey => NotificationFanoutOccurrenceKind.EventCancellation,
                NotificationFanoutRecipientTemplateFactory.SessionCancelledTemplateKey => NotificationFanoutOccurrenceKind.SessionCancellation,
                NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey or NotificationFanoutRecipientTemplateFactory.SessionUpdatedTemplateKey => NotificationFanoutOccurrenceKind.ImportantUpdate,
                NotificationFanoutOccurrenceCoordinationPolicy.EventReminderTemplateKey or NotificationFanoutOccurrenceCoordinationPolicy.SessionReminderTemplateKey => NotificationFanoutOccurrenceKind.Reminder,
                _ => throw new ArgumentOutOfRangeException(nameof(candidate))
            };
            DateTime notBefore = kind switch
            {
                NotificationFanoutOccurrenceKind.ImportantUpdate => candidate.OccurredAt.AddMinutes(5),
                NotificationFanoutOccurrenceKind.Reminder => candidate.RequestedNotBefore,
                _ => candidate.OccurredAt
            };
            return NotificationFanoutOccurrence.Create(
                candidate.OccurrenceId,
                candidate.TenantId,
                candidate.EventId,
                candidate.SessionId,
                candidate.OccurredAt,
                candidate.AudienceCutoffAt,
                candidate.AggregateVersion,
                candidate.ChangeSetJson,
                candidate.SafeBeforeSnapshotJson,
                candidate.SafeAfterSnapshotJson,
                candidate.TemplateKey,
                candidate.TemplateVersion,
                candidate.DeliveryPolicyId,
                candidate.PolicyVersion,
                Priority(kind),
                notBefore,
                candidate.SourceType,
                candidate.SourceId,
                candidate.SessionId.HasValue
                    ? $"event:{candidate.EventId:N}:session:{candidate.SessionId.Value:N}"
                    : $"event:{candidate.EventId:N}",
                kind == NotificationFanoutOccurrenceKind.ImportantUpdate ? notBefore : null);
        }

        public string Snapshot(string title, DateTime startsAt, bool sessionScoped = false) =>
            NotificationFanoutTemplateJson.Serialize(new NotificationFanoutSnapshotV1(
                title,
                sessionScoped ? $"{title} session" : null,
                new DateTimeOffset(startsAt),
                new DateTimeOffset(startsAt.AddHours(1)),
                "UTC",
                null));
    }
}
