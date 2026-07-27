// ABOUTME: PostgreSQL integration tests for EventRegistrationIntentRepository capacity and duplicate safety.
// ABOUTME: Verifies serializable registration creation keeps session capacity counters correct under concurrency and rollback.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventRegistrationIntentRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_ConcurrentCapacityOneRegistrations_CreatesSingleApprovedRow()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 6, sessionCapacity: 1);

        var results = await Task.WhenAll(scenario.UserIds.Select(userId => CreateRegistrationAsync(scenario, userId)));

        await using var verifyContext = fixture.CreateDbContext();
        var registrations = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .Where(registration => registration.EventSessionId == scenario.SessionId)
            .ToListAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var approvedCount = registrations.Count(registration => registration.ApprovalStatusId == (int)ApprovalStatusEnum.Approved);
        var waitlistedCount = registrations.Count(registration => registration.ApprovalStatusId == (int)ApprovalStatusEnum.Waitlisted);

        await Assert.That(results.Count(result => !result.HasWaitlistedSessions)).IsEqualTo(1);
        await Assert.That(results.Count(result => result.HasWaitlistedSessions)).IsEqualTo(scenario.UserIds.Count - 1);
        await Assert.That(registrations.Count).IsEqualTo(scenario.UserIds.Count);
        await Assert.That(approvedCount).IsEqualTo(1);
        await Assert.That(waitlistedCount).IsEqualTo(scenario.UserIds.Count - 1);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task AtprotoReconciliation_CompletedCreateThenOwnershipTombstoneAllowsSamePayloadRecreateAndDeduplicatesIdentity()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        RegistrationScenario scenario = await SeedRegistrationScenarioAsync(context, userCount: 1, sessionCapacity: 10);
        Guid attendeeId = scenario.UserIds.Single();
        EventRegistrationIntent eventIntent = NewIntent(scenario, attendeeId, RegistrationScopeEnum.Event);
        EventRegistrationIntent sessionIntent = NewIntent(scenario, attendeeId, RegistrationScopeEnum.SessionSelection);
        PrepareAtprotoIntent(eventIntent);
        PrepareAtprotoIntent(sessionIntent);
        context.EventRegistrationIntents.AddRange(eventIntent, sessionIntent);
        await context.SaveChangesAsync();
        var intentRepository = new EventRegistrationIntentRepository(context);

        IReadOnlyList<EventRegistrationIntent> beforeEventSettlement =
            await intentRepository.GetAtprotoReconciliationCandidatesAsync(null, 100, CancellationToken.None);
        await Assert.That(beforeEventSettlement).IsEmpty();

        AtprotoRecord eventRecord = await AddSettledEventOwnershipAsync(context, scenario);
        IReadOnlyList<EventRegistrationIntent> afterEventSettlement =
            await intentRepository.GetAtprotoReconciliationCandidatesAsync(null, 100, CancellationToken.None);
        await Assert.That(afterEventSettlement).Count().IsEqualTo(1);
        await Assert.That(afterEventSettlement.Single().UserId).IsEqualTo(attendeeId);

        var rsvpRecord = new AtprotoRecord
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:attendee",
            Collection = "community.lexicon.calendar.rsvp",
            RecordKey = "stable-rsvp",
            Uri = "at://did:plc:attendee/community.lexicon.calendar.rsvp/stable-rsvp",
            Cid = "bafy-rsvp",
            RecordJson = "{\"status\":\"community.lexicon.calendar.rsvp#going\"}",
            RecordHash = new string('a', 64),
            Direction = AtprotoRecordDirection.Outbound,
            Provenance = AtprotoRecordProvenance.LocalLifecycle,
            UpdatedAt = DateTime.UtcNow
        };
        context.AtprotoRecords.Add(rsvpRecord);
        context.AtprotoOutboundRecordOwnerships.Add(new AtprotoOutboundRecordOwnership
        {
            AtprotoRecordId = rsvpRecord.Id,
            TenantId = scenario.TenantId,
            UserId = attendeeId,
            SourceEntityType = "EventRegistrationIntent",
            SourceEntityId = eventIntent.Id,
            SourceVersion = eventIntent.ConcurrencyStamp,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        PdsSyncOutbox completed = NewRsvpOutbox(
            scenario,
            attendeeId,
            eventIntent,
            eventRecord,
            new string('b', 64),
            PdsSyncStatus.Completed);
        context.PdsSyncOutbox.Add(completed);
        await context.SaveChangesAsync();

        await Assert.That(await intentRepository.GetAtprotoReconciliationCandidatesAsync(
            null, 100, CancellationToken.None)).IsEmpty();

        rsvpRecord.TombstonedAt = DateTime.UtcNow;
        rsvpRecord.Cid = null;
        rsvpRecord.RecordJson = null;
        rsvpRecord.RecordHash = null;
        await context.SaveChangesAsync();
        await Assert.That(await intentRepository.GetAtprotoReconciliationCandidatesAsync(
            null, 100, CancellationToken.None)).Count().IsEqualTo(1);

        PdsSyncOutbox recreate = NewRsvpOutbox(
            scenario,
            attendeeId,
            eventIntent,
            eventRecord,
            completed.PayloadHash,
            PdsSyncStatus.Pending);
        var outboxRepository = new PdsSyncOutboxRepository(context);
        await outboxRepository.AddAsync(recreate, CancellationToken.None);
        await Assert.That(recreate.Id.Version).IsEqualTo(7);
        await Assert.That(await intentRepository.GetAtprotoReconciliationCandidatesAsync(
            null, 100, CancellationToken.None)).IsEmpty();

    }

    [Test]
    public async Task AtprotoActiveAttemptUniqueness_ConcurrentExactAttemptsPersistExactlyOneActiveRow()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        RegistrationScenario scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        Guid userId = scenario.UserIds.Single();
        Guid sourceVersion = await seedContext.Events
            .Where(value => value.Id == scenario.EventId)
            .Select(value => value.ConcurrencyStamp)
            .SingleAsync();
        string payloadHash = new('e', 64);

        bool[] results = await Task.WhenAll(
            PersistExactActiveAttemptAsync(scenario, userId, sourceVersion, payloadHash),
            PersistExactActiveAttemptAsync(scenario, userId, sourceVersion, payloadHash));

        await using var verifyContext = fixture.CreateDbContext();
        int activeCount = await verifyContext.PdsSyncOutbox
            .IgnoreQueryFilters()
            .CountAsync(value =>
                value.TenantId == scenario.TenantId
                && value.SourceEntityType == "Event"
                && value.SourceEntityId == scenario.EventId
                && value.SourceVersion == sourceVersion
                && value.Operation == PdsSyncOperation.Create
                && value.PayloadHash == payloadHash
                && (value.Status == PdsSyncStatus.Pending || value.Status == PdsSyncStatus.Processing)
                && value.SupersededAt == null);

        await Assert.That(results.Count(result => result)).IsEqualTo(1);
        await Assert.That(activeCount).IsEqualTo(1);
    }

    [Test]
    public async Task AtprotoReconciliation_RepeatedSweepAndRestartSuppressExactTerminalAttemptUntilStrongRefChanges()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        RegistrationScenario scenario = await SeedRegistrationScenarioAsync(context, userCount: 1, sessionCapacity: 10);
        Guid attendeeId = scenario.UserIds.Single();
        EventRegistrationIntent intent = NewIntent(scenario, attendeeId, RegistrationScopeEnum.Event);
        PrepareAtprotoIntent(intent);
        context.EventRegistrationIntents.Add(intent);
        AtprotoRecord eventRecord = await AddSettledEventOwnershipAsync(context, scenario);
        await context.SaveChangesAsync();

        string staleHash = new('c', 64);
        string changedHash = new('d', 64);
        PdsSyncOutbox deadLetter = NewRsvpOutbox(
            scenario,
            attendeeId,
            intent,
            eventRecord,
            staleHash,
            PdsSyncStatus.DeadLettered);
        context.PdsSyncOutbox.Add(deadLetter);
        await context.SaveChangesAsync();
        var repository = new PdsSyncOutboxRepository(context);

        await Assert.That(await repository.HasTerminalRsvpPublicationAttemptAsync(
            scenario.TenantId,
            attendeeId,
            scenario.EventId,
            intent.ConcurrencyStamp,
            PdsSyncOperation.Create,
            staleHash,
            "EventRegistrationIntent",
            "community.lexicon.calendar.rsvp",
            CancellationToken.None)).IsTrue();
        await Assert.That(await repository.HasTerminalRsvpPublicationAttemptAsync(
            scenario.TenantId,
            attendeeId,
            scenario.EventId,
            intent.ConcurrencyStamp,
            PdsSyncOperation.Create,
            changedHash,
            "EventRegistrationIntent",
            "community.lexicon.calendar.rsvp",
            CancellationToken.None)).IsFalse();

        var candidateRepository = new EventRegistrationIntentRepository(context);
        await Assert.That(await candidateRepository.GetAtprotoReconciliationCandidatesAsync(
            null, 100, CancellationToken.None)).IsEmpty();
        await Assert.That(await candidateRepository.GetAtprotoReconciliationCandidatesAsync(
            null, 100, CancellationToken.None)).IsEmpty();
        await using (ExploreDbContext restartedContext = fixture.CreateDbContext())
        {
            await Assert.That(await new EventRegistrationIntentRepository(restartedContext)
                .GetAtprotoReconciliationCandidatesAsync(null, 100, CancellationToken.None)).IsEmpty();
        }

        eventRecord.Cid = "bafy-event-changed";
        await context.SaveChangesAsync();
        await Assert.That(await candidateRepository.GetAtprotoReconciliationCandidatesAsync(
            null, 100, CancellationToken.None)).Count().IsEqualTo(1);

        PdsSyncOutbox changedAttempt = NewRsvpOutbox(
            scenario,
            attendeeId,
            intent,
            eventRecord,
            changedHash,
            PdsSyncStatus.Pending);
        await repository.SupersedePriorRsvpAsync(
            scenario.TenantId,
            attendeeId,
            scenario.EventId,
            "community.lexicon.calendar.rsvp",
            changedAttempt.Id,
            DateTime.UtcNow,
            CancellationToken.None);
        await repository.AddAsync(changedAttempt, CancellationToken.None);

        await Assert.That(deadLetter.Status).IsEqualTo(PdsSyncStatus.Superseded);
        await Assert.That(changedAttempt.Id.Version).IsEqualTo(7);
        await Assert.That(await new EventRegistrationIntentRepository(context)
            .GetAtprotoReconciliationCandidatesAsync(null, 100, CancellationToken.None)).IsEmpty();
    }

    [Test]
    public async Task AtprotoReconciliation_HighDuplicationDoesNotStarveLaterIdentityAcrossPages()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        RegistrationScenario scenario = await SeedRegistrationScenarioAsync(context, userCount: 2, sessionCapacity: 10);
        Guid duplicateUserId = scenario.UserIds[0];
        Guid laterUserId = scenario.UserIds[1];
        var days = Enumerable.Range(0, 8)
            .Select(index => new EventDay
            {
                Id = Guid.CreateVersion7(),
                EventId = scenario.EventId,
                Event = null!,
                LocalDate = new DateOnly(2026, 8, 2).AddDays(index),
                Label = $"ATProto duplicate day {index}",
                IsPublished = true,
                SortOrder = index + 1,
                AllowsDayScopeRegistration = true,
                TenantId = scenario.TenantId,
                Tenant = null!
            })
            .ToArray();
        context.EventDays.AddRange(days);
        await context.SaveChangesAsync();

        var duplicateIntents = new List<EventRegistrationIntent>();
        foreach (Guid dayId in new[] { scenario.EventDayId }.Concat(days.Select(day => day.Id)))
        {
            EventRegistrationIntent intent = NewIntent(scenario, duplicateUserId, RegistrationScopeEnum.Day);
            intent.Id = Guid.CreateVersion7();
            intent.SelectedEventDayId = dayId;
            PrepareAtprotoIntent(intent);
            duplicateIntents.Add(intent);
        }

        EventRegistrationIntent laterIntent = NewIntent(scenario, laterUserId, RegistrationScopeEnum.Event);
        laterIntent.Id = Guid.CreateVersion7();
        PrepareAtprotoIntent(laterIntent);
        context.EventRegistrationIntents.AddRange(duplicateIntents);
        context.EventRegistrationIntents.Add(laterIntent);
        await context.SaveChangesAsync();
        await AddSettledEventOwnershipAsync(context, scenario);
        var repository = new EventRegistrationIntentRepository(context);

        IReadOnlyList<EventRegistrationIntent> firstPage =
            await repository.GetAtprotoReconciliationCandidatesAsync(null, 1, CancellationToken.None);
        IReadOnlyList<EventRegistrationIntent> secondPage =
            await repository.GetAtprotoReconciliationCandidatesAsync(firstPage.Single().Id, 1, CancellationToken.None);

        await Assert.That(firstPage).Count().IsEqualTo(1);
        await Assert.That(firstPage.Single().UserId).IsEqualTo(duplicateUserId);
        await Assert.That(secondPage).Count().IsEqualTo(1);
        await Assert.That(secondPage.Single().UserId).IsEqualTo(laterUserId);
    }

    [Test]
    public async Task AtprotoPublication_LocalMutationAndOutboxRollbackTogether()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        RegistrationScenario scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        Guid userId = scenario.UserIds.Single();

        await using var context = CreateRetryingDbContext();
        Explore.Domain.Event eventEntity = await context.Events.SingleAsync(value => value.Id == scenario.EventId);
        PdsSyncOutbox outbox = NewEventOutbox(scenario, userId, eventEntity);
        var outboxRepository = new PdsSyncOutboxRepository(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EfCoreUnitOfWork(context)
            .ExecuteInTransactionAsync(async token =>
            {
                eventEntity.EventStatusId = (int)EventStatusEnum.Published;
                eventEntity.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(token);
                await outboxRepository.AddAsync(outbox, token);
                throw new InvalidOperationException("rollback-probe");
            }));

        await using var verifyContext = fixture.CreateDbContext();
        await Assert.That(await verifyContext.Events
            .Where(value => value.Id == scenario.EventId)
            .Select(value => value.EventStatusId)
            .SingleAsync()).IsEqualTo((int)EventStatusEnum.Draft);
        await Assert.That(await verifyContext.PdsSyncOutbox
            .IgnoreQueryFilters()
            .AnyAsync(value => value.Id == outbox.Id)).IsFalse();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)RegistrationScopeEnum.Event)]
    [Arguments((int)RegistrationScopeEnum.Day)]
    [Arguments((int)RegistrationScopeEnum.SessionSelection)]
    public async Task CreateWithChildrenAndCapacityAsyncAppliesCapacityForEveryRegistrationScope(int scopeId)
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 2, sessionCapacity: 1);
        var scope = (RegistrationScopeEnum)scopeId;

        var first = await CreateRegistrationAsync(scenario, scenario.UserIds[0], scope);
        var second = await CreateRegistrationAsync(scenario, scenario.UserIds[1], scope);

        await using var verifyContext = fixture.CreateDbContext();
        var intentStates = await verifyContext.EventRegistrationIntents
            .Where(intent => intent.EventId == scenario.EventId)
            .OrderBy(intent => intent.UserId)
            .Select(intent => intent.ApprovalStatusId)
            .ToArrayAsync();
        var childStates = await verifyContext.EventRegistrations
            .Where(registration => registration.EventSessionId == scenario.SessionId)
            .OrderBy(registration => registration.UserId)
            .Select(registration => registration.ApprovalStatusId)
            .ToArrayAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(first.HasWaitlistedSessions).IsFalse();
        await Assert.That(second.HasWaitlistedSessions).IsTrue();
        await Assert.That(intentStates.Count(status => status == (int)ApprovalStatusEnum.Approved)).IsEqualTo(1);
        await Assert.That(intentStates.Count(status => status == (int)ApprovalStatusEnum.Waitlisted)).IsEqualTo(1);
        await Assert.That(childStates.Count(status => status == (int)ApprovalStatusEnum.Approved)).IsEqualTo(1);
        await Assert.That(childStates.Count(status => status == (int)ApprovalStatusEnum.Waitlisted)).IsEqualTo(1);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_OverridesSuppliedCoverageWithOccurrenceTime()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        Guid userId = scenario.UserIds.Single();
        EventRegistrationIntent intent = NewIntent(
            scenario,
            userId,
            RegistrationScopeEnum.SessionSelection);
        EventRegistration child = NewRegistrationChild(scenario, userId);
        child.CoverageEstablishedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTimeOffset occurredAt = new(2026, 8, 5, 14, 30, 0, TimeSpan.Zero);

        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(context);
        await new EfCoreUnitOfWork(context).ExecuteSerializableAsync(
            cancellationToken => repository.CreateWithChildrenAndCapacityAsync(
                intent,
                [child],
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Guid.CreateVersion7(),
                occurredAt,
                EventRegistrationActorProvenance.Attendee,
                userId,
                cancellationToken));

        await using var verifyContext = fixture.CreateDbContext();
        DateTime persistedCoverage = await verifyContext.EventRegistrations
            .Where(registration => registration.Id == child.Id)
            .Select(registration => registration.CoverageEstablishedAt)
            .SingleAsync();

        await Assert.That(persistedCoverage).IsEqualTo(occurredAt.UtcDateTime);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateSessionSelectionIntent_ReturnsExistingIntent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationAsync(scenario, userId);
        await using var duplicateContext = CreateRetryingDbContext();

        var duplicate = await ExecuteCapacityCreationAsync(
            duplicateContext,
            NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection),
            [NewRegistrationChild(scenario, userId)],
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventSessionId == scenario.SessionId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(currentAttendees).IsEqualTo(1);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(intentCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateSessionSelectionIntent_DoesNotCreateDuplicateEmailDispatchRow()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationWithEmailDispatchAsync(scenario, userId);
        var duplicateIntent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);

        var duplicate = await CreateRegistrationWithEmailDispatchAsync(scenario, userId, duplicateIntent);

        await using var verifyContext = fixture.CreateDbContext();
        var outboxRows = await verifyContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
            .Where(dispatch => dispatch.TenantId == scenario.TenantId
                && dispatch.EventId == scenario.EventId
                && dispatch.RecipientUserId == userId
                && dispatch.Kind == EmailDispatchKind.RegistrationConfirmation)
            .Select(dispatch => new
            {
                dispatch.SourceType,
                dispatch.SourceId,
                dispatch.RegistrationIntentId,
                dispatch.NotificationIntentId,
                dispatch.Status
            })
            .ToListAsync();
        var notificationIntents = await verifyContext.NotificationIntents
            .IgnoreQueryFilters()
            .Where(intent => intent.TenantId == scenario.TenantId
                && intent.EventId == scenario.EventId
                && intent.RecipientUserId == userId
                && intent.TemplateKey == "registration.confirmation")
            .Select(intent => intent.Id)
            .ToListAsync();
        var emailDeliveryCount = await verifyContext.NotificationDeliveries
            .CountAsync(delivery => delivery.TenantId == scenario.TenantId
                && notificationIntents.Contains(delivery.NotificationIntentId)
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email);
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventSessionId == scenario.SessionId);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(notificationIntents.Count).IsEqualTo(1);
        await Assert.That(emailDeliveryCount).IsEqualTo(1);
        await Assert.That(outboxRows.Count).IsEqualTo(1);
        var outboxRow = outboxRows.Single();
        await Assert.That(outboxRow.SourceType).IsEqualTo(EventLifecycleEmailOutboxFactory.RegistrationIntentSourceType);
        await Assert.That(outboxRow.SourceId).IsEqualTo(first.Intent.Id);
        await Assert.That(outboxRow.RegistrationIntentId).IsEqualTo(first.Intent.Id);
        await Assert.That(outboxRow.NotificationIntentId).IsEqualTo(notificationIntents.Single());
        await Assert.That(outboxRow.Status).IsEqualTo(EmailDispatchStatus.Pending);
        await Assert.That(outboxRow.SourceId).IsNotEqualTo(duplicateIntent.Id);
        await Assert.That(currentAttendees).IsEqualTo(1);
        await Assert.That(registrationCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateEventScopeIntent_ReturnsExistingIntent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationAsync(scenario, userId, RegistrationScopeEnum.Event);
        await using var duplicateContext = CreateRetryingDbContext();

        var duplicate = await ExecuteCapacityCreationAsync(
            duplicateContext,
            NewIntent(scenario, userId, RegistrationScopeEnum.Event),
            [NewRegistrationChild(scenario, userId, scenario.SecondarySessionId)],
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var attendeeCounts = await GetSessionAttendeeCountsAsync(verifyContext, scenario);
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId && registration.UserId == userId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId
                && registrationIntent.UserId == userId
                && registrationIntent.RegistrationScopeId == (int)RegistrationScopeEnum.Event);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(attendeeCounts[scenario.SessionId]).IsEqualTo(1);
        await Assert.That(attendeeCounts[scenario.SecondarySessionId]).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(intentCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_DuplicateDayScopeIntent_ReturnsExistingIntent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var userId = scenario.UserIds.Single();
        var first = await CreateRegistrationAsync(scenario, userId, RegistrationScopeEnum.Day);
        await using var duplicateContext = CreateRetryingDbContext();

        var duplicate = await ExecuteCapacityCreationAsync(
            duplicateContext,
            NewIntent(scenario, userId, RegistrationScopeEnum.Day),
            [NewRegistrationChild(scenario, userId, scenario.SecondarySessionId)],
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var attendeeCounts = await GetSessionAttendeeCountsAsync(verifyContext, scenario);
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId && registration.UserId == userId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId
                && registrationIntent.UserId == userId
                && registrationIntent.RegistrationScopeId == (int)RegistrationScopeEnum.Day
                && registrationIntent.SelectedEventDayId == scenario.EventDayId);

        await Assert.That(first.WasExisting).IsFalse();
        await Assert.That(duplicate.WasExisting).IsTrue();
        await Assert.That(duplicate.Intent.Id).IsEqualTo(first.Intent.Id);
        await Assert.That(attendeeCounts[scenario.SessionId]).IsEqualTo(1);
        await Assert.That(attendeeCounts[scenario.SecondarySessionId]).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(1);
        await Assert.That(intentCount).IsEqualTo(1);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_WhenChildUniqueConstraintFails_RollsBackCapacityCounter()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        await using var registrationContext = CreateRetryingDbContext();
        var userId = scenario.UserIds.Single();
        var intent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        var duplicateChildren = new[]
        {
            NewRegistrationChild(scenario, userId),
            NewRegistrationChild(scenario, userId)
        };

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await ExecuteCapacityCreationAsync(
                registrationContext,
                intent,
                duplicateChildren,
                CancellationToken.None);
        });

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventSessionId == scenario.SessionId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId);

        await Assert.That(currentAttendees).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(0);
        await Assert.That(intentCount).IsEqualTo(0);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_WhenChildSessionBelongsToDifferentEvent_RejectsBeforeCapacityCounter()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        var otherScenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        await using var registrationContext = CreateRetryingDbContext();
        var userId = scenario.UserIds.Single();
        var intent = NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        var crossEventChild = NewRegistrationChild(scenario, userId, otherScenario.SessionId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await ExecuteCapacityCreationAsync(
                registrationContext,
                intent,
                [crossEventChild],
                CancellationToken.None);
        });

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var otherCurrentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == otherScenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var registrationCount = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .CountAsync(registration => registration.EventId == scenario.EventId
                || registration.EventSessionId == otherScenario.SessionId);
        var intentCount = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .CountAsync(registrationIntent => registrationIntent.EventId == scenario.EventId);

        await Assert.That(exception.Message).Contains("does not belong to the registration intent tenant and event");
        await Assert.That(currentAttendees).IsEqualTo(0);
        await Assert.That(otherCurrentAttendees).IsEqualTo(0);
        await Assert.That(registrationCount).IsEqualTo(0);
        await Assert.That(intentCount).IsEqualTo(0);
    }

    [Test]
    public async Task CreateWithChildrenAndCapacityAsync_WithoutCallerTransaction_Throws()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 1, sessionCapacity: 10);
        await using var registrationContext = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(registrationContext);
        var userId = scenario.UserIds.Single();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await repository.CreateWithChildrenAndCapacityAsync(
                NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection),
                [NewRegistrationChild(scenario, userId)],
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                EventRegistrationActorProvenance.Attendee,
                userId,
                CancellationToken.None);
        });

        await Assert.That(exception.Message).IsEqualTo(
            "Capacity-aware registration creation requires a caller-owned serializable transaction.");
    }

    [Test]
    public async Task GetEarliestApprovedReminderSessionAsyncUsesApprovedParentCoverageAndStableStartIdOrder()
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context = fixture.CreateDbContext();
        RegistrationScenario scenario = await SeedRegistrationScenarioAsync(context, userCount: 3, sessionCapacity: 10);
        DateTimeOffset cutoff = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        Guid lowerSessionId = Guid.Parse("10000000-0000-7000-8000-000000000001");
        Guid upperSessionId = Guid.Parse("10000000-0000-7000-8000-000000000002");
        EventSession lower = NewSession(
            scenario.TenantId, scenario.EventId, scenario.EventDayId, "Lower id", 10,
            cutoff.AddDays(2), cutoff.AddDays(2).AddHours(1));
        lower.Id = lowerSessionId;
        lower.EventSessionStatusId = (int)EventSessionStatusEnum.Published;
        EventSession upper = NewSession(
            scenario.TenantId, scenario.EventId, scenario.EventDayId, "Upper id", 10,
            cutoff.AddDays(2), cutoff.AddDays(2).AddHours(1));
        upper.Id = upperSessionId;
        upper.EventSessionStatusId = (int)EventSessionStatusEnum.Published;
        EventSession started = NewSession(
            scenario.TenantId, scenario.EventId, scenario.EventDayId, "Started", 10,
            cutoff.AddMinutes(-1), cutoff.AddMinutes(30));
        started.EventSessionStatusId = (int)EventSessionStatusEnum.Published;
        EventSession unpublished = NewSession(
            scenario.TenantId, scenario.EventId, scenario.EventDayId, "Unpublished", 10,
            cutoff.AddMinutes(1), cutoff.AddHours(1));
        EventSession unrelated = NewSession(
            scenario.TenantId, scenario.EventId, scenario.EventDayId, "Unrelated", 10,
            cutoff.AddMinutes(2), cutoff.AddHours(1));
        unrelated.EventSessionStatusId = (int)EventSessionStatusEnum.Published;
        EventSession daySession = await context.EventSessions.SingleAsync(value => value.Id == scenario.SecondarySessionId);
        daySession.EventSessionStatusId = (int)EventSessionStatusEnum.Published;
        context.EventSessions.AddRange(lower, upper, started, unpublished, unrelated);

        EventRegistrationIntent eventIntent = NewIntent(scenario, scenario.UserIds[0], RegistrationScopeEnum.Event);
        EventRegistrationIntent dayIntent = NewIntent(scenario, scenario.UserIds[1], RegistrationScopeEnum.Day);
        EventRegistrationIntent selectionIntent = NewIntent(scenario, scenario.UserIds[2], RegistrationScopeEnum.SessionSelection);
        eventIntent.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        dayIntent.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        selectionIntent.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        context.EventRegistrationIntents.AddRange(eventIntent, dayIntent, selectionIntent);

        EventRegistration EventChild(EventRegistrationIntent intent, Guid sessionId, bool deleted = false) => new()
        {
            Id = Guid.CreateVersion7(),
            EventId = intent.EventId,
            Event = null!,
            UserId = intent.UserId,
            User = null!,
            EventSessionId = sessionId,
            EventSession = null!,
            EventRegistrationIntentId = intent.Id,
            EventRegistrationIntent = null,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            TenantId = intent.TenantId,
            Tenant = null!,
            CoverageEstablishedAt = cutoff.UtcDateTime,
            IsDeleted = deleted,
            DeletedAt = deleted ? cutoff.UtcDateTime : null
        };

        EventRegistration dayChild = EventChild(dayIntent, daySession.Id);
        context.EventRegistrations.AddRange(
            EventChild(eventIntent, lower.Id),
            EventChild(eventIntent, lower.Id, deleted: true),
            EventChild(eventIntent, upper.Id),
            EventChild(eventIntent, started.Id),
            EventChild(eventIntent, unpublished.Id),
            dayChild,
            EventChild(selectionIntent, upper.Id));
        await context.SaveChangesAsync();
        var repository = new EventRegistrationIntentRepository(context);

        EventSession? eventScope = await repository.GetEarliestApprovedReminderSessionAsync(
            scenario.TenantId, eventIntent.Id, cutoff, CancellationToken.None);
        EventSession? dayScope = await repository.GetEarliestApprovedReminderSessionAsync(
            scenario.TenantId, dayIntent.Id, cutoff, CancellationToken.None);
        EventSession? explicitScope = await repository.GetEarliestApprovedReminderSessionAsync(
            scenario.TenantId, selectionIntent.Id, cutoff, CancellationToken.None);

        await Assert.That(eventScope!.Id).IsEqualTo(lowerSessionId);
        await Assert.That(dayScope!.Id).IsEqualTo(daySession.Id);
        await Assert.That(explicitScope!.Id).IsEqualTo(upperSessionId);

        dayIntent.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
        await context.SaveChangesAsync();
        await Assert.That(await repository.GetEarliestApprovedReminderSessionAsync(
            scenario.TenantId, dayIntent.Id, cutoff, CancellationToken.None)).IsNull();
        dayIntent.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        dayChild.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
        await context.SaveChangesAsync();
        await Assert.That(await repository.GetEarliestApprovedReminderSessionAsync(
            scenario.TenantId, dayIntent.Id, cutoff, CancellationToken.None)).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task GetRegisteredUserFanoutBatchAsyncReturnsOnlyLiveApprovalStates()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationScenarioAsync(seedContext, userCount: 7, sessionCapacity: 10);
        var statusCases = new (int StatusId, bool IsDeleted)[]
        {
            ((int)ApprovalStatusEnum.Pending, false),
            ((int)ApprovalStatusEnum.Approved, false),
            ((int)ApprovalStatusEnum.Waitlisted, false),
            ((int)ApprovalStatusEnum.Rejected, false),
            ((int)ApprovalStatusEnum.Cancelled, false),
            ((int)ApprovalStatusEnum.Revoked, false),
            ((int)ApprovalStatusEnum.Approved, true)
        };

        var intents = scenario.UserIds
            .Zip(statusCases)
            .Select(item =>
            {
                var intent = NewIntent(scenario, item.First, RegistrationScopeEnum.Event);
                intent.ApprovalStatusId = item.Second.StatusId;
                intent.IsDeleted = item.Second.IsDeleted;
                return intent;
            })
            .ToArray();
        seedContext.EventRegistrationIntents.AddRange(intents);
        await seedContext.SaveChangesAsync();

        await using var queryContext = CreateRetryingDbContext();
        var repository = new EventRegistrationIntentRepository(queryContext);
        var userIds = await repository.GetRegisteredUserFanoutBatchAsync(
            scenario.TenantId,
            scenario.EventId,
            afterUserId: null,
            pageSize: 20,
            CancellationToken.None);

        await Assert.That(userIds.ToHashSet().SetEquals(scenario.UserIds.Take(3))).IsTrue();
    }

    private async Task<EventRegistrationIntentCreationResult> CreateRegistrationAsync(
        RegistrationScenario scenario,
        Guid userId,
        RegistrationScopeEnum scope = RegistrationScopeEnum.SessionSelection)
    {
        await using var context = CreateRetryingDbContext();
        return await ExecuteCapacityCreationAsync(
            context,
            NewIntent(scenario, userId, scope),
            [NewRegistrationChild(scenario, userId)],
            CancellationToken.None);
    }

    private async Task<EventRegistrationIntentCreationResult> CreateRegistrationWithEmailDispatchAsync(
        RegistrationScenario scenario,
        Guid userId,
        EventRegistrationIntent? suppliedIntent = null)
    {
        await using var context = CreateRetryingDbContext();
        var registrationRepository = new EventRegistrationIntentRepository(context);
        var notificationRepository = new NotificationIntentRepository(context);
        var unitOfWork = new EfCoreUnitOfWork(context);
        var materializer = new RecipientNotificationMaterializer(notificationRepository, unitOfWork);
        var intent = suppliedIntent ?? NewIntent(scenario, userId, RegistrationScopeEnum.SessionSelection);
        EmailDispatchOutbox email = NewRegistrationConfirmationDispatch(
            scenario,
            userId,
            intent.Id);
        RecipientNotificationMaterialization notification = NewRegistrationNotificationMaterialization(
            intent,
            email);
        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;

        try
        {
            return await unitOfWork.ExecuteSerializableAsync(
                async ct =>
                {
                    EventRegistrationIntentCreationResult result =
                        await registrationRepository.CreateWithChildrenAndCapacityAsync(
                            intent,
                            [NewRegistrationChild(scenario, userId)],
                            (int)ApprovalStatusEnum.Approved,
                            (int)ApprovalStatusEnum.Waitlisted,
                            occurrenceId,
                            occurredAt,
                            EventRegistrationActorProvenance.Attendee,
                            userId,
                            ct);
                    await materializer.MaterializeInCurrentTransactionAsync(notification, ct);
                    return result;
                },
                CancellationToken.None);
        }
        catch (EventRegistrationIntentConflictException)
        {
            EventRegistrationIntent? winningIntent = await registrationRepository.FindExistingAsync(
                intent.EventId,
                intent.UserId,
                intent.RegistrationScopeId,
                intent.SelectedEventDayId,
                CancellationToken.None);
            if (winningIntent is null)
            {
                throw;
            }

            return ExistingCreationResult(winningIntent, occurrenceId, occurredAt);
        }
    }

    private static async Task<EventRegistrationIntentCreationResult> ExecuteCapacityCreationAsync(
        ExploreDbContext context,
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        CancellationToken cancellationToken)
    {
        var repository = new EventRegistrationIntentRepository(context);
        var unitOfWork = new EfCoreUnitOfWork(context);
        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;

        try
        {
            return await unitOfWork.ExecuteSerializableAsync(
                ct => repository.CreateWithChildrenAndCapacityAsync(
                    intent,
                    children,
                    (int)ApprovalStatusEnum.Approved,
                    (int)ApprovalStatusEnum.Waitlisted,
                    occurrenceId,
                    occurredAt,
                    EventRegistrationActorProvenance.Attendee,
                    intent.UserId,
                    ct),
                cancellationToken);
        }
        catch (EventRegistrationIntentConflictException)
        {
            EventRegistrationIntent? winningIntent = await repository.FindExistingAsync(
                intent.EventId,
                intent.UserId,
                intent.RegistrationScopeId,
                intent.SelectedEventDayId,
                cancellationToken);
            if (winningIntent is null)
            {
                throw;
            }

            return ExistingCreationResult(winningIntent, occurrenceId, occurredAt);
        }
    }

    private static EventRegistrationIntentCreationResult ExistingCreationResult(
        EventRegistrationIntent intent,
        Guid occurrenceId,
        DateTimeOffset occurredAt)
    {
        return new EventRegistrationIntentCreationResult(
            intent,
            [],
            new EventRegistrationTransitionResult(
                Changed: false,
                ParentIntentId: intent.Id,
                PreviousStatus: intent.ApprovalStatusId,
                FinalStatus: intent.ApprovalStatusId,
                TransitionReason: EventRegistrationTransitionReason.NoChange,
                OccurrenceId: occurrenceId,
                OccurredAt: occurredAt,
                ActorProvenance: EventRegistrationActorProvenance.Attendee,
                ActorUserId: intent.UserId,
                ChildTransitions: []),
            WasExisting: true);
    }

    private ExploreDbContext CreateRetryingDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Persistence integration test concurrent registration context.");
        return context;
    }

    private static async Task<RegistrationScenario> SeedRegistrationScenarioAsync(
        ExploreDbContext context,
        int userCount,
        int sessionCapacity)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = "Registration Intent Tenant",
            Slug = "registration-intent-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var users = Enumerable.Range(0, userCount + 1)
            .Select(index => NewUser($"registration-intent-{index}"))
            .ToList();

        context.Tenants.Add(tenant);
        context.Users.AddRange(users);
        context.TenantUsers.AddRange(users.Select(user => new TenantUser
        {
            Id = Guid.CreateVersion7(),
            Tenant = tenant,
            UserId = user.Id,
            User = user,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        }));
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Pii = new ActorPii { DisplayName = "Registration Intent Actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = users[0].Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventId = Guid.NewGuid();
        var @event = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Registration Intent Event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Draft,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            TotalViews = 0
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        var eventDay = new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            LocalDate = new DateOnly(2026, 8, 1),
            Label = "Registration Intent Day",
            IsPublished = true,
            SortOrder = 0,
            AllowsDayScopeRegistration = true,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context.EventDays.Add(eventDay);
        await context.SaveChangesAsync();

        var session = NewSession(
            tenant.Id,
            eventId,
            eventDay.Id,
            "Capacity Limited Session",
            sessionCapacity,
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
        var secondarySession = NewSession(
            tenant.Id,
            eventId,
            eventDay.Id,
            "Alternate Capacity Limited Session",
            sessionCapacity,
            new DateTimeOffset(2026, 8, 1, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));
        context.EventSessions.AddRange(session, secondarySession);
        await context.SaveChangesAsync();

        return new RegistrationScenario(
            tenant.Id,
            eventId,
            eventDay.Id,
            session.Id,
            secondarySession.Id,
            users.Skip(1).Select(user => user.Id).ToList(),
            users.Skip(1).ToDictionary(user => user.Id, user => user.Email!));
    }

    private static EventSession NewSession(
        Guid tenantId,
        Guid eventId,
        Guid eventDayId,
        string title,
        int sessionCapacity,
        DateTimeOffset startTime,
        DateTimeOffset endTime)
    {
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            EventDayId = eventDayId,
            EventDay = null,
            Title = title,
            TenantId = tenantId,
            Tenant = null!,
            MaxAudienceAttendees = sessionCapacity,
            CurrentAudienceAttendees = 0
        };
        session.Reschedule(startTime, endTime, "UTC", new EventScheduleProjectionCalculator());

        return session;
    }

    private static User NewUser(string prefix)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Pii = new UserPii
            {
                Email = $"{prefix}-{Guid.NewGuid():N}@example.com",
                FirstName = "Registration",
                LastName = "Intent"
            },
            EmailVerified = true,
        };
    }

    private static EventRegistrationIntent NewIntent(
        RegistrationScenario scenario,
        Guid userId,
        RegistrationScopeEnum scope)
    {
        return new EventRegistrationIntent
        {
            Id = Guid.NewGuid(),
            EventId = scenario.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScopeId = (int)scope,
            RegistrationScope = null!,
            SelectedEventDayId = scope == RegistrationScopeEnum.Day ? scenario.EventDayId : null,
            SelectedEventDay = null,
            TenantId = scenario.TenantId,
            Tenant = null!
        };
    }

    private static void PrepareAtprotoIntent(EventRegistrationIntent intent)
    {
        intent.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
        intent.ApprovalStatus = null!;
        intent.ConcurrencyStamp = Guid.CreateVersion7();
        intent.CreatedAt = DateTime.UtcNow;
    }

    private static async Task<AtprotoRecord> AddSettledEventOwnershipAsync(
        ExploreDbContext context,
        RegistrationScenario scenario)
    {
        var source = await context.Events
            .Where(value => value.Id == scenario.EventId)
            .Select(value => new { value.Actor.UserId, value.ConcurrencyStamp })
            .SingleAsync();
        DateTime now = DateTime.UtcNow;
        string recordKey = $"event-{scenario.EventId:N}";
        var record = new AtprotoRecord
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:event-owner",
            Collection = "community.lexicon.calendar.event",
            RecordKey = recordKey,
            Uri = $"at://did:plc:event-owner/community.lexicon.calendar.event/{recordKey}",
            Cid = "bafy-event-settled",
            RecordJson = "{\"name\":\"Registration Intent Event\"}",
            RecordHash = new string('f', 64),
            Direction = AtprotoRecordDirection.Outbound,
            Provenance = AtprotoRecordProvenance.LocalLifecycle,
            UpdatedAt = now
        };
        context.AtprotoRecords.Add(record);
        context.AtprotoOutboundRecordOwnerships.Add(new AtprotoOutboundRecordOwnership
        {
            AtprotoRecordId = record.Id,
            TenantId = scenario.TenantId,
            UserId = source.UserId!.Value,
            SourceEntityType = "Event",
            SourceEntityId = scenario.EventId,
            SourceVersion = source.ConcurrencyStamp,
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
        return record;
    }

    private static PdsSyncOutbox NewRsvpOutbox(
        RegistrationScenario scenario,
        Guid attendeeId,
        EventRegistrationIntent intent,
        AtprotoRecord eventRecord,
        string payloadHash,
        PdsSyncStatus status)
    {
        DateTime now = DateTime.UtcNow;
        var outbox = new PdsSyncOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = scenario.TenantId,
            UserId = attendeeId,
            Did = "did:plc:attendee",
            Collection = "community.lexicon.calendar.rsvp",
            RecordKey = "stable-rsvp",
            Operation = PdsSyncOperation.Create,
            Payload = "{\"status\":\"community.lexicon.calendar.rsvp#going\"}",
            PayloadHash = payloadHash,
            IdempotencyKey = $"rsvp:{Guid.CreateVersion7():N}",
            PdsHost = "https://pds.example",
            SourceEntityType = "EventRegistrationIntent",
            SourceEntityId = intent.Id,
            SourceVersion = intent.ConcurrencyStamp,
            DependsOnAtprotoRecordId = eventRecord.Id,
            DependsOnCid = eventRecord.Cid,
            Status = status,
            CreatedAt = now,
            MaxRetries = 10
        };
        if (status == PdsSyncStatus.Completed)
        {
            outbox.ProcessedAt = now;
            outbox.SettledUri = $"at://{outbox.Did}/{outbox.Collection}/{outbox.RecordKey}";
            outbox.SettledCid = "bafy-rsvp";
        }
        else if (status == PdsSyncStatus.DeadLettered)
        {
            outbox.RetryCount = 1;
            outbox.LastError = "payload_changed";
            outbox.DeadLetteredAt = now;
        }

        return outbox;
    }

    private static PdsSyncOutbox NewEventOutbox(
        RegistrationScenario scenario,
        Guid userId,
        Explore.Domain.Event eventEntity,
        Guid? sourceVersion = null,
        string? payloadHash = null)
    {
        string hash = payloadHash ?? new string('9', 64);
        return new PdsSyncOutbox
        {
            Id = Guid.CreateVersion7(),
            TenantId = scenario.TenantId,
            UserId = userId,
            Did = "did:plc:rollback-owner",
            Collection = "community.lexicon.calendar.event",
            RecordKey = $"event-{scenario.EventId:N}",
            Operation = PdsSyncOperation.Create,
            Payload = "{\"name\":\"Rollback probe\"}",
            PayloadHash = hash,
            IdempotencyKey = $"event:{Guid.CreateVersion7():N}",
            PdsHost = "https://pds.example",
            SourceEntityType = "Event",
            SourceEntityId = scenario.EventId,
            SourceVersion = sourceVersion ?? eventEntity.ConcurrencyStamp,
            Status = PdsSyncStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            MaxRetries = 10
        };
    }

    private async Task<bool> PersistExactActiveAttemptAsync(
        RegistrationScenario scenario,
        Guid userId,
        Guid sourceVersion,
        string payloadHash)
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        Explore.Domain.Event eventEntity = await context.Events.SingleAsync(value => value.Id == scenario.EventId);
        PdsSyncOutbox outbox = NewEventOutbox(scenario, userId, eventEntity, sourceVersion, payloadHash);
        try
        {
            await new PdsSyncOutboxRepository(context).AddAsync(outbox, CancellationToken.None);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    private static EventRegistration NewRegistrationChild(
        RegistrationScenario scenario,
        Guid userId,
        Guid? sessionId = null)
    {
        return new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = scenario.EventId,
            Event = null!,
            UserId = userId,
            User = null!,
            EventSessionId = sessionId ?? scenario.SessionId,
            EventSession = null!,
            TenantId = scenario.TenantId,
            Tenant = null!
        };
    }

    private static EmailDispatchOutbox NewRegistrationConfirmationDispatch(
        RegistrationScenario scenario,
        Guid userId,
        Guid registrationIntentId)
    {
        return new EventLifecycleEmailOutboxFactory().CreateRegistrationConfirmation(
            scenario.TenantId,
            userId,
            scenario.EventId,
            registrationIntentId,
            scenario.RecipientEmails[userId],
            "Registration Intent Event");
    }

    private static RecipientNotificationMaterialization NewRegistrationNotificationMaterialization(
        EventRegistrationIntent registrationIntent,
        EmailDispatchOutbox email)
    {
        return new RecipientNotificationMaterialization(
            Guid.CreateVersion7(),
            new NotificationIntentDraft(
                Explore.Application.Notifications.NotificationCategory.RegistrationLifecycle,
                TenantId: registrationIntent.TenantId,
                RecipientKind: "User",
                TemplateKey: "registration.confirmation",
                SafePayloadReference: $"event-registration-intent:{registrationIntent.Id}",
                DeduplicationKey:
                    $"event-registration-intent:{registrationIntent.Id:N}:registration-confirmation",
                CorrelationId: registrationIntent.Id.ToString("D"),
                UserId: registrationIntent.UserId,
                EventId: registrationIntent.EventId),
            NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            "registration_status",
            new RecipientInAppNotificationDraft(
                (int)NotificationTypeEnum.RegistrationConfirmed,
                "Registration created",
                "Your registration for Registration Intent Event was created.",
                (int)ActorTypeEnum.User,
                (int)NotificationReasonEnum.System,
                (int)NotificationEntityTypeEnum.EventRegistration,
                registrationIntent.Id.ToString("D")),
            email,
            IncludeEmailChannel: true,
            EmailRequired: false,
            PreferenceCategoryCode: NotificationPreferenceCategoryCodes.RegistrationStatus,
            LinkAllowed: false);
    }

    private static async Task<Dictionary<Guid, int?>> GetSessionAttendeeCountsAsync(
        ExploreDbContext context,
        RegistrationScenario scenario)
    {
        return await context.EventSessions
            .Where(session => session.Id == scenario.SessionId || session.Id == scenario.SecondarySessionId)
            .Select(session => new { session.Id, session.CurrentAudienceAttendees })
            .ToDictionaryAsync(session => session.Id, session => session.CurrentAudienceAttendees);
    }

    private sealed record RegistrationScenario(
        Guid TenantId,
        Guid EventId,
        Guid EventDayId,
        Guid SessionId,
        Guid SecondarySessionId,
        IReadOnlyList<Guid> UserIds,
        IReadOnlyDictionary<Guid, string> RecipientEmails);
}
