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
    public async Task DiscontinuousPredecessorStartsUnmergedOccurrenceAndExactReplayUsesSameBoundary()
    {
        var fixture = new Fixture();
        string predecessorBefore = fixture.Snapshot("Predecessor before", fixture.At);
        string predecessorAfter = fixture.Snapshot("Predecessor after", fixture.At.AddHours(1));
        NotificationFanoutOccurrence predecessor = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            sessionId: null,
            NotificationFanoutChangeField.StartTime,
            predecessorBefore,
            predecessorAfter);
        string incomingBefore = fixture.Snapshot("Discontinuous incoming before", fixture.At.AddHours(3));
        string incomingAfter = fixture.Snapshot("Incoming after", fixture.At.AddHours(4));
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null,
            NotificationFanoutChangeField.EndTime,
            incomingBefore,
            incomingAfter);
        fixture.Pending(predecessor);

        NotificationFanoutOccurrenceCoordinationResult first = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);
        NotificationFanoutRecipientTemplate firstTemplate = new NotificationFanoutRecipientTemplateFactory()
            .Parse(first.Occurrence);

        await Assert.That(firstTemplate.ChangeSet.Fields).IsEquivalentTo([NotificationFanoutChangeField.EndTime]);
        await Assert.That(first.Occurrence.SafeBeforeSnapshotJson).IsEqualTo(incomingBefore);
        int createdCount = fixture.CreatedOccurrences.Count;
        int pointerCount = fixture.OutboxPointers.Count;
        fixture.Replay(first.Occurrence);
        fixture.Predecessors(first.Occurrence.Id, predecessor);

        NotificationFanoutOccurrenceCoordinationResult replay = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);

        await Assert.That(replay.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.SourceReplay);
        await Assert.That(fixture.CreatedOccurrences).Count().IsEqualTo(createdCount);
        await Assert.That(fixture.OutboxPointers).Count().IsEqualTo(pointerCount);
        NotificationFanoutOccurrenceCandidate tampered = incoming with
        {
            SafeBeforeSnapshotJson = fixture.Snapshot("Tampered discontinuity", fixture.At.AddHours(5))
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(tampered));
    }

    [Test]
    public async Task EventTimezoneCoalescingKeepsEarliestCarriedTimesAndIncomingTimesForNewSessions()
    {
        var fixture = new Fixture();
        Guid carriedSessionId = Guid.CreateVersion7();
        Guid newSessionId = Guid.CreateVersion7();
        var carriedEarliest = new NotificationFanoutSessionDisplayTimeV1(
            carriedSessionId,
            "Carried",
            new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero),
            null);
        var carriedIncomingBefore = carriedEarliest with
        {
            StartsAt = new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2))
        };
        var carriedLatest = carriedEarliest with
        {
            StartsAt = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.FromHours(1))
        };
        var newIncomingBefore = new NotificationFanoutSessionDisplayTimeV1(
            newSessionId,
            "New",
            new DateTimeOffset(2026, 10, 25, 2, 45, 0, TimeSpan.FromHours(2)),
            null);
        var newLatest = newIncomingBefore with
        {
            StartsAt = new DateTimeOffset(2026, 10, 25, 1, 45, 0, TimeSpan.FromHours(1))
        };
        string earliestBefore = TimezoneSnapshot("UTC", carriedEarliest);
        string intermediate = TimezoneSnapshot("Europe/Brussels", carriedIncomingBefore);
        NotificationFanoutOccurrence existing = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            sessionId: null,
            NotificationFanoutChangeField.Timezone,
            earliestBefore,
            intermediate);
        string incomingBefore = TimezoneSnapshot("Europe/Brussels", carriedIncomingBefore, newIncomingBefore);
        string latestAfter = TimezoneSnapshot("Europe/London", newLatest, carriedLatest);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(4),
            sessionId: null,
            NotificationFanoutChangeField.Timezone,
            incomingBefore,
            latestAfter);
        fixture.Pending(existing);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(result.Occurrence);

        Guid[] actualIds = template.Before.SessionDisplayTimes!.Select(value => value.SessionId).ToArray();
        await Assert.That(actualIds.SequenceEqual(actualIds.Order())).IsTrue();
        await Assert.That(template.Before.SessionDisplayTimes.Single(value => value.SessionId == newSessionId))
            .IsEqualTo(newIncomingBefore);
        await Assert.That(template.Before.SessionDisplayTimes.Single(value => value.SessionId == carriedSessionId))
            .IsEqualTo(carriedEarliest);
        await Assert.That(template.After.SessionDisplayTimes).IsEquivalentTo([newLatest, carriedLatest]);

        int createdCount = fixture.CreatedOccurrences.Count;
        int pointerCount = fixture.OutboxPointers.Count;
        fixture.Replay(result.Occurrence);
        fixture.Predecessors(result.Occurrence.Id, existing);

        NotificationFanoutOccurrenceCoordinationResult replay = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);

        await Assert.That(replay.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.SourceReplay);
        await Assert.That(replay.Occurrence.Id).IsEqualTo(result.Occurrence.Id);
        await Assert.That(fixture.CreatedOccurrences).Count().IsEqualTo(createdCount);
        await Assert.That(fixture.OutboxPointers).Count().IsEqualTo(pointerCount);
    }

    [Test]
    public async Task EnrichedTimezoneSourceReplayRejectsChangedIntersectingIncomingBeforeSnapshot()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence predecessor = fixture.Persisted(
            EnrichedTimezoneCandidate(fixture, fixture.At, fixture.SessionA));
        var incomingBefore = new NotificationFanoutSessionDisplayTimeV1(
            fixture.SessionA,
            "Included session",
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(1)));
        var incomingAfter = incomingBefore with
        {
            StartsAt = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.FromHours(1)),
            EndsAt = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero)
        };
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null,
            NotificationFanoutChangeField.Timezone,
            TimezoneSnapshot("Europe/Brussels", incomingBefore),
            TimezoneSnapshot("Europe/London", incomingAfter));
        fixture.Pending(predecessor);
        NotificationFanoutOccurrenceCoordinationResult first = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);
        fixture.Replay(first.Occurrence);
        fixture.Predecessors(first.Occurrence.Id, predecessor);
        NotificationFanoutOccurrenceCandidate tampered = incoming with
        {
            SafeBeforeSnapshotJson = TimezoneSnapshot(
                "Europe/Brussels",
                incomingBefore with { SessionTitle = "Altered replay title" })
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(tampered));
    }

    [Test]
    public async Task SessionCancellationSupersedesPendingEnrichedEventTimezoneOccurrence()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence timezoneOccurrence = fixture.Persisted(
            EnrichedTimezoneCandidate(fixture, fixture.At, fixture.SessionA));
        fixture.Pending(timezoneOccurrence);
        NotificationFanoutOccurrenceCandidate cancellation = fixture.Candidate(
            NotificationFanoutOccurrenceKind.SessionCancellation,
            fixture.At.AddMinutes(1),
            fixture.SessionA);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(cancellation);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive);
        await Assert.That(timezoneOccurrence.State).IsEqualTo(NotificationFanoutOccurrenceState.Superseded);
        await Assert.That(timezoneOccurrence.SupersededByOccurrenceId).IsEqualTo(cancellation.OccurrenceId);
        await fixture.EmailSuppressionRepository.Received(1).SuppressPreHandoffAsync(
            timezoneOccurrence.TenantId,
            timezoneOccurrence.Id,
            cancellation.OccurredAt,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExistingSessionCancellationBlocksLaterEnrichedEventTimezoneOccurrence()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence cancellation = fixture.Persisted(
            NotificationFanoutOccurrenceKind.SessionCancellation,
            fixture.At,
            fixture.SessionA);
        fixture.Pending(cancellation);
        NotificationFanoutOccurrenceCandidate timezone = EnrichedTimezoneCandidate(
            fixture,
            fixture.At.AddMinutes(1),
            fixture.SessionA);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(timezone);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.Superseded);
        await Assert.That(result.ActiveOccurrenceId).IsEqualTo(cancellation.Id);
        await Assert.That(result.Occurrence.State).IsEqualTo(NotificationFanoutOccurrenceState.Superseded);
        await Assert.That(result.Occurrence.SupersededByOccurrenceId).IsEqualTo(cancellation.Id);
        await Assert.That(fixture.OutboxPointers).IsEmpty();
    }

    [Test]
    public async Task UnrelatedSessionCancellationDoesNotCompeteWithEnrichedEventTimezoneOccurrence()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence timezoneOccurrence = fixture.Persisted(
            EnrichedTimezoneCandidate(fixture, fixture.At, fixture.SessionA));
        fixture.Pending(timezoneOccurrence);
        NotificationFanoutOccurrenceCandidate cancellation = fixture.Candidate(
            NotificationFanoutOccurrenceKind.SessionCancellation,
            fixture.At.AddMinutes(1),
            fixture.SessionB);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(cancellation);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive);
        await Assert.That(timezoneOccurrence.State).IsEqualTo(NotificationFanoutOccurrenceState.Pending);
        await fixture.EmailSuppressionRepository.DidNotReceiveWithAnyArgs()
            .SuppressPreHandoffAsync(default, default, default, default);
    }

    [Test]
    public async Task LegacyThenEnrichedTimezoneBoundaryStartsSeparateOccurrenceWithoutCoalescing()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence legacy = fixture.Persisted(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At,
            sessionId: null,
            NotificationFanoutChangeField.StartTime);
        var beforeSession = new NotificationFanoutSessionDisplayTimeV1(
            fixture.SessionA,
            "Session",
            new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 10, 25, 3, 30, 0, TimeSpan.FromHours(2)));
        string mixedBefore = TimezoneSnapshot("Europe/Brussels", beforeSession);
        var afterSession = beforeSession with
        {
            StartsAt = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.FromHours(1)),
            EndsAt = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero)
        };
        string mixedAfter = TimezoneSnapshot("Europe/London", afterSession);
        NotificationFanoutOccurrenceCandidate incoming = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null,
            NotificationFanoutChangeField.Timezone,
            mixedBefore,
            mixedAfter);
        fixture.Pending(legacy);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(incoming);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(result.Occurrence);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive);
        await Assert.That(template.ChangeSet.Fields).IsEquivalentTo([NotificationFanoutChangeField.Timezone]);
        await Assert.That(template.Before.SessionDisplayTimes).IsNotNull();
        await Assert.That(template.After.SessionDisplayTimes).IsNotNull();
        await Assert.That(fixture.OutboxPointers).Count().IsEqualTo(1);
    }

    [Test]
    public async Task EnrichedThenLegacyTimezoneBoundaryStartsSeparateOccurrenceWithoutCoalescing()
    {
        var fixture = new Fixture();
        NotificationFanoutOccurrence enriched = fixture.Persisted(
            EnrichedTimezoneCandidate(fixture, fixture.At, fixture.SessionA));
        fixture.Pending(enriched);
        NotificationFanoutOccurrenceCandidate legacy = fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            fixture.At.AddMinutes(1),
            sessionId: null,
            NotificationFanoutChangeField.StartTime);

        NotificationFanoutOccurrenceCoordinationResult result = await fixture.Coordinator
            .CoordinateInCurrentTransactionAsync(legacy);
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(result.Occurrence);

        await Assert.That(result.Outcome).IsEqualTo(NotificationFanoutOccurrenceCoordinationOutcome.NewlyActive);
        await Assert.That(template.ChangeSet.Fields).IsEquivalentTo([NotificationFanoutChangeField.StartTime]);
        await Assert.That(template.Before.SessionDisplayTimes).IsNull();
        await Assert.That(template.After.SessionDisplayTimes).IsNull();
        await Assert.That(fixture.OutboxPointers).Count().IsEqualTo(1);
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

    private static string TimezoneSnapshot(
        string timezone,
        params NotificationFanoutSessionDisplayTimeV1[] sessions) =>
        NotificationFanoutTemplateJson.Serialize(new NotificationFanoutSnapshotV1(
            "Immutable event",
            SessionTitle: null,
            StartsAt: null,
            EndsAt: null,
            Timezone: timezone,
            Location: null,
            SessionDisplayTimes: sessions));

    private static NotificationFanoutOccurrenceCandidate EnrichedTimezoneCandidate(
        Fixture fixture,
        DateTime occurredAt,
        Guid includedSessionId)
    {
        var before = new NotificationFanoutSessionDisplayTimeV1(
            includedSessionId,
            "Included session",
            new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero));
        var after = before with
        {
            StartsAt = new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2)),
            EndsAt = new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(1))
        };
        return fixture.Candidate(
            NotificationFanoutOccurrenceKind.ImportantUpdate,
            occurredAt,
            sessionId: null,
            NotificationFanoutChangeField.Timezone,
            TimezoneSnapshot("UTC", before),
            TimezoneSnapshot("Europe/Brussels", after));
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
            OccurrenceRepository.GetDirectPredecessorsForCoordinationAsync(
                    Arg.Any<Guid>(),
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

        public void Predecessors(Guid replacementOccurrenceId, params NotificationFanoutOccurrence[] predecessors)
        {
            OccurrenceRepository.GetDirectPredecessorsForCoordinationAsync(
                    TenantId,
                    EventId,
                    replacementOccurrenceId,
                    Arg.Any<CancellationToken>())
                .Returns(predecessors);
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
