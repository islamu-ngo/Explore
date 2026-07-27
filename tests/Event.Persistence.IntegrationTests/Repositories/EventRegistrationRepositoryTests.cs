// ABOUTME: PostgreSQL integration tests for EventRegistrationRepository cancellation behavior.
// ABOUTME: Verifies cancellation remains atomic when Npgsql retry execution strategies are enabled.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Registration;
using Explore.Domain.Services.Scheduling;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventRegistrationRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CancelAndReleaseCapacityAsync_WithRetryingExecutionStrategy_CancelsRegistrationAndReleasesCapacity()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext);
        await using var cancelContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(cancelContext);

        var cancelled = await ExecuteCancellationAsync(
            cancelContext,
            repository,
            scenario.RegistrationId,
            scenario.UserId);

        await Assert.That(cancelled.Changed).IsTrue();

        await using var verifyContext = fixture.CreateDbContext();
        var registration = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .SingleAsync(r => r.Id == scenario.RegistrationId);
        var intent = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == scenario.IntentId);
        var currentAttendees = await verifyContext.EventSessions
            .Where(s => s.Id == scenario.SessionId)
            .Select(s => s.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(registration.IsDeleted).IsTrue();
        await Assert.That(registration.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Cancelled);
        await Assert.That(intent.IsDeleted).IsTrue();
        await Assert.That(intent.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Cancelled);
        await Assert.That(currentAttendees).IsEqualTo(0);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)ApprovalStatusEnum.Pending, 0)]
    [Arguments((int)ApprovalStatusEnum.Approved, 0)]
    [Arguments((int)ApprovalStatusEnum.Rejected, 1)]
    [Arguments((int)ApprovalStatusEnum.Waitlisted, 1)]
    public async Task CancelAndReleaseCapacityAsyncWhenLastChildIsCancelledRemovesEveryLiveEntitlement(
        int initialApprovalStatusId,
        int expectedCurrentAttendees)
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext, initialApprovalStatusId);
        await using var cancelContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(cancelContext);

        var cancelled = await ExecuteCancellationAsync(
            cancelContext,
            repository,
            scenario.RegistrationId,
            scenario.UserId);

        await using var verifyContext = fixture.CreateDbContext();
        var childIsDeleted = await verifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .Where(registration => registration.Id == scenario.RegistrationId)
            .Select(registration => new { registration.IsDeleted, registration.ApprovalStatusId })
            .SingleAsync();
        var intentState = await verifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .Where(intent => intent.Id == scenario.IntentId)
            .Select(intent => new { intent.IsDeleted, intent.ApprovalStatusId })
            .SingleAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var parentRemainsLive = !intentState.IsDeleted
            && intentState.ApprovalStatusId is
                (int)ApprovalStatusEnum.Pending or
                (int)ApprovalStatusEnum.Approved or
                (int)ApprovalStatusEnum.Waitlisted;

        await Assert.That(cancelled.Changed).IsTrue();
        await Assert.That(childIsDeleted.IsDeleted).IsTrue();
        await Assert.That(childIsDeleted.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Cancelled);
        await Assert.That(intentState.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Cancelled);
        await Assert.That(parentRemainsLive).IsFalse();
        await Assert.That(RegistrationApprovalStatusRules.IsLiveForLocationDisclosure(
            childIsDeleted.ApprovalStatusId,
            childIsDeleted.IsDeleted)).IsFalse();
        await Assert.That(currentAttendees).IsEqualTo(expectedCurrentAttendees);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)ApprovalStatusEnum.Pending)]
    [Arguments((int)ApprovalStatusEnum.Approved)]
    public async Task CancelAndReleaseCapacityAsyncOnlyTerminatesParentAfterLastLiveChildIsCancelled(
        int initialApprovalStatusId)
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(
            seedContext,
            initialApprovalStatusId,
            includeSecondRegistration: true);
        var secondRegistrationId = scenario.SecondRegistrationId!.Value;

        await using (var firstCancelContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(firstCancelContext);
            var cancelled = await ExecuteCancellationAsync(
                firstCancelContext,
                repository,
                scenario.RegistrationId,
                scenario.UserId);
            await Assert.That(cancelled.Changed).IsTrue();
        }

        await using (var partialVerifyContext = fixture.CreateDbContext())
        {
            var firstChildIsDeleted = await partialVerifyContext.EventRegistrations
                .IgnoreQueryFilters()
                .Where(registration => registration.Id == scenario.RegistrationId)
                .Select(registration => new { registration.IsDeleted, registration.ApprovalStatusId })
                .SingleAsync();
            var secondChildIsLive = await partialVerifyContext.EventRegistrations
                .Where(registration => registration.Id == secondRegistrationId)
                .AnyAsync();
            var parentIsLive = await partialVerifyContext.EventRegistrationIntents
                .Where(intent => intent.Id == scenario.IntentId)
                .AnyAsync();
            var firstCurrentAttendees = await partialVerifyContext.EventSessions
                .Where(session => session.Id == scenario.SessionId)
                .Select(session => session.CurrentAudienceAttendees)
                .SingleAsync();
            var secondCurrentAttendees = await partialVerifyContext.EventSessions
                .Where(session => session.Id == scenario.SecondSessionId!.Value)
                .Select(session => session.CurrentAudienceAttendees)
                .SingleAsync();

            await Assert.That(firstChildIsDeleted.IsDeleted).IsTrue();
            await Assert.That(firstChildIsDeleted.ApprovalStatusId)
                .IsEqualTo((int)ApprovalStatusEnum.Cancelled);
            await Assert.That(secondChildIsLive).IsTrue();
            await Assert.That(parentIsLive).IsTrue();
            await Assert.That(firstCurrentAttendees).IsEqualTo(0);
            await Assert.That(secondCurrentAttendees).IsEqualTo(1);
        }

        await using (var lastCancelContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(lastCancelContext);
            var cancelled = await ExecuteCancellationAsync(
                lastCancelContext,
                repository,
                secondRegistrationId,
                scenario.UserId);
            await Assert.That(cancelled.Changed).IsTrue();
        }

        await using var finalVerifyContext = fixture.CreateDbContext();
        var secondChildIsDeleted = await finalVerifyContext.EventRegistrations
            .IgnoreQueryFilters()
            .Where(registration => registration.Id == secondRegistrationId)
            .Select(registration => new { registration.IsDeleted, registration.ApprovalStatusId })
            .SingleAsync();
        var intentState = await finalVerifyContext.EventRegistrationIntents
            .IgnoreQueryFilters()
            .Where(intent => intent.Id == scenario.IntentId)
            .Select(intent => new { intent.IsDeleted, intent.ApprovalStatusId })
            .SingleAsync();
        var parentRemainsLive = !intentState.IsDeleted
            && intentState.ApprovalStatusId is
                (int)ApprovalStatusEnum.Pending or
                (int)ApprovalStatusEnum.Approved or
                (int)ApprovalStatusEnum.Waitlisted;
        var firstFinalCurrentAttendees = await finalVerifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        var secondFinalCurrentAttendees = await finalVerifyContext.EventSessions
            .Where(session => session.Id == scenario.SecondSessionId!.Value)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(secondChildIsDeleted.IsDeleted).IsTrue();
        await Assert.That(secondChildIsDeleted.ApprovalStatusId)
            .IsEqualTo((int)ApprovalStatusEnum.Cancelled);
        await Assert.That(intentState.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Cancelled);
        await Assert.That(parentRemainsLive).IsFalse();
        await Assert.That(firstFinalCurrentAttendees).IsEqualTo(0);
        await Assert.That(secondFinalCurrentAttendees).IsEqualTo(0);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task CancelAndReleaseCapacityAsyncWhenOwnerChangesAfterAuthorizationFailsClosed()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext);
        var replacementUser = new User
        {
            Id = Guid.NewGuid(),
            Pii = new UserPii
            {
                Email = $"registration-owner-change-{Guid.NewGuid():N}@example.com",
                FirstName = "Replacement",
                LastName = "Owner"
            }
        };
        seedContext.Users.Add(replacementUser);
        var registration = await seedContext.EventRegistrations
            .SingleAsync(item => item.Id == scenario.RegistrationId);
        registration.UserId = replacementUser.Id;
        await seedContext.SaveChangesAsync();

        await using var cancelContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(cancelContext);

        var cancelled = await ExecuteCancellationAsync(
            cancelContext,
            repository,
            scenario.RegistrationId,
            scenario.UserId);

        await using var verifyContext = fixture.CreateDbContext();
        var persistedRegistration = await verifyContext.EventRegistrations
            .SingleAsync(item => item.Id == scenario.RegistrationId);
        var parentIsLive = await verifyContext.EventRegistrationIntents
            .AnyAsync(item => item.Id == scenario.IntentId);
        var currentAttendees = await verifyContext.EventSessions
            .Where(item => item.Id == scenario.SessionId)
            .Select(item => item.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(cancelled.Changed).IsFalse();
        await Assert.That(persistedRegistration.UserId).IsEqualTo(replacementUser.Id);
        await Assert.That(persistedRegistration.IsDeleted).IsFalse();
        await Assert.That(persistedRegistration.ApprovalStatusId)
            .IsEqualTo((int)ApprovalStatusEnum.Approved);
        await Assert.That(parentIsLive).IsTrue();
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task CancelAndReleaseCapacityAsyncWithoutCallerTransactionThrows()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext);
        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.CancelAndReleaseCapacityAsync(
                scenario.RegistrationId,
                scenario.UserId,
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                EventRegistrationActorProvenance.Attendee,
                scenario.UserId,
                CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo(
            "Capacity-aware registration writes require a caller-owned serializable transaction.");
    }

    [Test]
    public async Task UpdateAndAdjustCapacityAsyncWithoutCallerTransactionThrows()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext);
        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(context);
        var registration = await repository.GetById(scenario.RegistrationId);
        registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Rejected;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAndAdjustCapacityAsync(
                registration,
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                EventRegistrationActorProvenance.Organizer,
                actorUserId: null,
                cancellationToken: CancellationToken.None));

        await Assert.That(exception.Message).IsEqualTo(
            "Capacity-aware registration writes require a caller-owned serializable transaction.");
    }

    [Test]
    public async Task CancelAndReleaseCapacityAsyncWhenApplicationWorkFailsRollsBackAggregateAndCapacity()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext);
        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(context);
        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;

        await Assert.That(async () => await new EfCoreUnitOfWork(context)
            .ExecuteSerializableAsync<EventRegistrationTransitionResult>(
                async ct =>
                {
                    await repository.CancelAndReleaseCapacityAsync(
                        scenario.RegistrationId,
                        scenario.UserId,
                        occurrenceId,
                        occurredAt,
                        EventRegistrationActorProvenance.Attendee,
                        scenario.UserId,
                        ct);
                    throw new InvalidOperationException("rollback probe");
                }))
            .Throws<InvalidOperationException>();

        await using var verifyContext = fixture.CreateDbContext();
        var registration = await verifyContext.EventRegistrations
            .SingleAsync(item => item.Id == scenario.RegistrationId);
        var intent = await verifyContext.EventRegistrationIntents
            .SingleAsync(item => item.Id == scenario.IntentId);
        var currentAttendees = await verifyContext.EventSessions
            .Where(item => item.Id == scenario.SessionId)
            .Select(item => item.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(registration.IsDeleted).IsFalse();
        await Assert.That(registration.ApprovalStatusId)
            .IsEqualTo((int)ApprovalStatusEnum.Approved);
        await Assert.That(intent.IsDeleted).IsFalse();
        await Assert.That(intent.ApprovalStatusId)
            .IsEqualTo((int)ApprovalStatusEnum.Approved);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task UpdateAndAdjustCapacityAsyncWhenApplicationWorkFailsRollsBackAggregateAndCapacity()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext, (int)ApprovalStatusEnum.Pending);
        await using var context = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(context);
        var registration = await repository.GetById(scenario.RegistrationId);
        registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Rejected;
        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;

        await Assert.That(async () => await new EfCoreUnitOfWork(context)
            .ExecuteSerializableAsync<EventRegistrationTransitionResult>(
                async ct =>
                {
                    await repository.UpdateAndAdjustCapacityAsync(
                        registration,
                        occurrenceId,
                        occurredAt,
                        EventRegistrationActorProvenance.Organizer,
                        actorUserId: null,
                        cancellationToken: ct);
                    throw new InvalidOperationException("rollback probe");
                }))
            .Throws<InvalidOperationException>();

        await using var verifyContext = fixture.CreateDbContext();
        var persistedRegistration = await verifyContext.EventRegistrations
            .SingleAsync(item => item.Id == scenario.RegistrationId);
        var intent = await verifyContext.EventRegistrationIntents
            .SingleAsync(item => item.Id == scenario.IntentId);
        var currentAttendees = await verifyContext.EventSessions
            .Where(item => item.Id == scenario.SessionId)
            .Select(item => item.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(persistedRegistration.ApprovalStatusId)
            .IsEqualTo((int)ApprovalStatusEnum.Pending);
        await Assert.That(intent.ApprovalStatusId)
            .IsEqualTo((int)ApprovalStatusEnum.Pending);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncPendingToRejectedReleasesCapacityOnlyOnce()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext, (int)ApprovalStatusEnum.Pending);
        EventRegistrationTransitionResult firstTransition;

        await using (var updateContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(updateContext);
            var registration = await repository.GetById(scenario.RegistrationId);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Rejected;

            firstTransition = await ExecuteUpdateAsync(updateContext, repository, registration);
        }

        Guid replacementId = firstTransition.ChildTransitions.Single().RegistrationId;
        await using (var repeatContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(repeatContext);
            var registration = await repository.GetById(replacementId);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Rejected;

            await ExecuteUpdateAsync(repeatContext, repository, registration);
        }

        await using var verifyContext = fixture.CreateDbContext();
        var persistedStatusId = await verifyContext.EventRegistrations
            .Where(registration => registration.Id == replacementId)
            .Select(registration => registration.ApprovalStatusId)
            .SingleAsync();
        var historicalRowIsDeleted = await verifyContext.EventRegistrations
            .IgnoreAllFilters("Replacement history verification requires the superseded registration row.")
            .Where(registration => registration.Id == scenario.RegistrationId)
            .Select(registration => registration.IsDeleted)
            .SingleAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(persistedStatusId).IsEqualTo((int)ApprovalStatusEnum.Rejected);
        await Assert.That(historicalRowIsDeleted).IsTrue();
        await Assert.That(currentAttendees).IsEqualTo(0);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncConcurrentPendingTransitionsReleaseCapacityOnlyOnce()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext, (int)ApprovalStatusEnum.Pending);
        await using var firstContext = CreateRetryingDbContext();
        await using var secondContext = CreateRetryingDbContext();
        var firstRepository = new EventRegistrationRepository(firstContext);
        var secondRepository = new EventRegistrationRepository(secondContext);
        var firstRegistration = await firstRepository.GetById(scenario.RegistrationId);
        var secondRegistration = await secondRepository.GetById(scenario.RegistrationId);
        firstRegistration!.ApprovalStatusId = (int)ApprovalStatusEnum.Rejected;
        secondRegistration!.ApprovalStatusId = (int)ApprovalStatusEnum.Rejected;

        await ExecuteUpdateAsync(firstContext, firstRepository, firstRegistration);
        await Assert.That(async () => await ExecuteUpdateAsync(
                secondContext,
                secondRepository,
                secondRegistration))
            .Throws<ConcurrencyConflictException>();

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(currentAttendees).IsEqualTo(0);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncEnteringPendingReservesCapacity()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext, (int)ApprovalStatusEnum.Waitlisted);
        await using var updateContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(updateContext);
        var registration = await repository.GetById(scenario.RegistrationId);
        registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Pending;

        await ExecuteUpdateAsync(updateContext, repository, registration);

        await using var verifyContext = fixture.CreateDbContext();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(currentAttendees).IsEqualTo(2);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncEnteringApprovedWhenFullRemainsWaitlisted()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(
            seedContext,
            (int)ApprovalStatusEnum.Waitlisted,
            sessionMaxAudienceAttendees: 1);
        await using var updateContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(updateContext);
        var registration = await repository.GetById(scenario.RegistrationId);
        registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;

        EventRegistrationTransitionResult transition =
            await ExecuteUpdateAsync(updateContext, repository, registration);
        Guid replacementId = transition.ChildTransitions.Single().RegistrationId;

        await using var verifyContext = fixture.CreateDbContext();
        var persistedStatusId = await verifyContext.EventRegistrations
            .Where(item => item.Id == replacementId)
            .Select(item => item.ApprovalStatusId)
            .SingleAsync();
        var historicalRowIsDeleted = await verifyContext.EventRegistrations
            .IgnoreAllFilters("Replacement history verification requires the superseded registration row.")
            .Where(item => item.Id == scenario.RegistrationId)
            .Select(item => item.IsDeleted)
            .SingleAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(persistedStatusId).IsEqualTo((int)ApprovalStatusEnum.Waitlisted);
        await Assert.That(historicalRowIsDeleted).IsTrue();
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncRecomputesParentAcrossMixedLiveChildren()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(
            seedContext,
            (int)ApprovalStatusEnum.Pending,
            includeSecondRegistration: true);

        await using (var firstUpdateContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(firstUpdateContext);
            var registration = await repository.GetById(scenario.RegistrationId);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
            await ExecuteUpdateAsync(firstUpdateContext, repository, registration);
        }

        await using (var mixedVerifyContext = fixture.CreateDbContext())
        {
            var parentStatusId = await mixedVerifyContext.EventRegistrationIntents
                .Where(intent => intent.Id == scenario.IntentId)
                .Select(intent => intent.ApprovalStatusId)
                .SingleAsync();
            await Assert.That(parentStatusId).IsEqualTo((int)ApprovalStatusEnum.Pending);
        }

        await using (var secondUpdateContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(secondUpdateContext);
            var registration = await repository.GetById(scenario.SecondRegistrationId!.Value);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
            await ExecuteUpdateAsync(secondUpdateContext, repository, registration);
        }

        await using var finalVerifyContext = fixture.CreateDbContext();
        var finalParentStatusId = await finalVerifyContext.EventRegistrationIntents
            .Where(intent => intent.Id == scenario.IntentId)
            .Select(intent => intent.ApprovalStatusId)
            .SingleAsync();
        await Assert.That(finalParentStatusId).IsEqualTo((int)ApprovalStatusEnum.Approved);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncWaitlistedToApprovedRecomputesParent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(
            seedContext,
            (int)ApprovalStatusEnum.Waitlisted);
        await using var updateContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(updateContext);
        var registration = await repository.GetById(scenario.RegistrationId);
        registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;

        await ExecuteUpdateAsync(updateContext, repository, registration);

        await using var verifyContext = fixture.CreateDbContext();
        var parentStatusId = await verifyContext.EventRegistrationIntents
            .Where(intent => intent.Id == scenario.IntentId)
            .Select(intent => intent.ApprovalStatusId)
            .SingleAsync();
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();
        await Assert.That(parentStatusId).IsEqualTo((int)ApprovalStatusEnum.Approved);
        await Assert.That(currentAttendees).IsEqualTo(2);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncRevocationIsTerminalAndRecomputesAllTerminalParent()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(
            seedContext,
            (int)ApprovalStatusEnum.Approved,
            includeSecondRegistration: true);
        Guid revokedFirstRegistrationId;

        await using (var firstRevokeContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(firstRevokeContext);
            var registration = await repository.GetById(scenario.RegistrationId);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Revoked;
            EventRegistrationTransitionResult transition =
                await ExecuteUpdateAsync(firstRevokeContext, repository, registration);
            revokedFirstRegistrationId = transition.ChildTransitions.Single().RegistrationId;
        }

        await using (var mixedVerifyContext = fixture.CreateDbContext())
        {
            var parentStatusId = await mixedVerifyContext.EventRegistrationIntents
                .Where(intent => intent.Id == scenario.IntentId)
                .Select(intent => intent.ApprovalStatusId)
                .SingleAsync();
            var firstCurrentAttendees = await mixedVerifyContext.EventSessions
                .Where(session => session.Id == scenario.SessionId)
                .Select(session => session.CurrentAudienceAttendees)
                .SingleAsync();
            await Assert.That(parentStatusId).IsEqualTo((int)ApprovalStatusEnum.Approved);
            await Assert.That(firstCurrentAttendees).IsEqualTo(0);
        }

        await using (var reopenContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(reopenContext);
            var registration = await repository.GetById(revokedFirstRegistrationId);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Approved;
            await Assert.That(async () => await ExecuteUpdateAsync(
                    reopenContext,
                    repository,
                    registration))
                .Throws<InvalidOperationException>();
        }

        await using (var secondRevokeContext = CreateRetryingDbContext())
        {
            var repository = new EventRegistrationRepository(secondRevokeContext);
            var registration = await repository.GetById(scenario.SecondRegistrationId!.Value);
            registration!.ApprovalStatusId = (int)ApprovalStatusEnum.Revoked;
            await ExecuteUpdateAsync(secondRevokeContext, repository, registration);
        }

        await using var finalVerifyContext = fixture.CreateDbContext();
        var parent = await finalVerifyContext.EventRegistrationIntents
            .SingleAsync(intent => intent.Id == scenario.IntentId);
        var childStates = await finalVerifyContext.EventRegistrations
            .Where(registration => registration.EventRegistrationIntentId == scenario.IntentId)
            .Select(registration => new { registration.ApprovalStatusId, registration.IsDeleted })
            .ToArrayAsync();
        var currentAttendees = await finalVerifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId
                || session.Id == scenario.SecondSessionId!.Value)
            .Select(session => session.CurrentAudienceAttendees)
            .ToArrayAsync();

        await Assert.That(parent.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Revoked);
        await Assert.That(parent.IsDeleted).IsFalse();
        await Assert.That(childStates.Length).IsEqualTo(2);
        await Assert.That(childStates.All(child =>
            child.ApprovalStatusId == (int)ApprovalStatusEnum.Revoked
            && !child.IsDeleted)).IsTrue();
        await Assert.That(childStates.All(child =>
            !RegistrationApprovalStatusRules.IsLiveForLocationDisclosure(
                child.ApprovalStatusId,
                child.IsDeleted))).IsTrue();
        await Assert.That(currentAttendees.All(count => count == 0)).IsTrue();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task UpdateAndAdjustCapacityAsyncRejectsParentIntentReassignmentWithoutStaleAggregate()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var scenario = await SeedRegistrationAsync(seedContext);
        var registrationIdentity = await seedContext.EventRegistrations
            .Where(registration => registration.Id == scenario.RegistrationId)
            .Select(registration => new
            {
                registration.EventId,
                registration.UserId,
                registration.TenantId
            })
            .SingleAsync();
        var replacementIntent = new EventRegistrationIntent
        {
            Id = Guid.NewGuid(),
            EventId = registrationIdentity.EventId,
            Event = null!,
            UserId = registrationIdentity.UserId,
            User = null!,
            RegistrationScopeId = (int)RegistrationScopeEnum.Event,
            RegistrationScope = null!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Pending,
            TenantId = registrationIdentity.TenantId,
            Tenant = null!
        };
        seedContext.EventRegistrationIntents.Add(replacementIntent);
        await seedContext.SaveChangesAsync();

        await using var updateContext = CreateRetryingDbContext();
        var repository = new EventRegistrationRepository(updateContext);
        var registration = await repository.GetById(scenario.RegistrationId);
        registration!.EventRegistrationIntentId = replacementIntent.Id;

        await Assert.That(async () => await ExecuteUpdateAsync(
                updateContext,
                repository,
                registration))
            .Throws<InvalidOperationException>();

        await using var verifyContext = fixture.CreateDbContext();
        var persistedIntentId = await verifyContext.EventRegistrations
            .Where(item => item.Id == scenario.RegistrationId)
            .Select(item => item.EventRegistrationIntentId)
            .SingleAsync();
        var parentStates = await verifyContext.EventRegistrationIntents
            .Where(intent => intent.Id == scenario.IntentId || intent.Id == replacementIntent.Id)
            .ToDictionaryAsync(intent => intent.Id, intent => intent.ApprovalStatusId);
        var currentAttendees = await verifyContext.EventSessions
            .Where(session => session.Id == scenario.SessionId)
            .Select(session => session.CurrentAudienceAttendees)
            .SingleAsync();

        await Assert.That(persistedIntentId).IsEqualTo(scenario.IntentId);
        await Assert.That(parentStates[scenario.IntentId]).IsEqualTo((int)ApprovalStatusEnum.Approved);
        await Assert.That(parentStates[replacementIntent.Id]).IsEqualTo((int)ApprovalStatusEnum.Pending);
        await Assert.That(currentAttendees).IsEqualTo(1);
    }

    [Test]
    public async Task GetRegistrationsByEventWithDetailsPaged_ReturnsOnlyRequestedEventRows()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();
        var firstScenario = await SeedRegistrationAsync(seedContext);
        var secondScenario = await SeedRegistrationAsync(seedContext);
        await using var queryContext = fixture.CreateDbContext();
        var repository = new EventRegistrationRepository(queryContext);

        var (items, totalCount) = await repository.GetRegistrationsByEventWithDetailsPaged(
            firstScenario.EventId,
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        await Assert.That(totalCount).IsEqualTo(1);
        await Assert.That(items.Count).IsEqualTo(1);
        await Assert.That(items[0].EventId).IsEqualTo(firstScenario.EventId);
        await Assert.That(items[0].EventId).IsNotEqualTo(secondScenario.EventId);
        await Assert.That(items[0].User?.Pii).IsNotNull();
        await Assert.That(items[0].EventSession?.Event).IsNotNull();
    }

    private static Task<EventRegistrationTransitionResult> ExecuteCancellationAsync(
        ExploreDbContext context,
        EventRegistrationRepository repository,
        Guid registrationId,
        Guid expectedOwnerUserId)
    {
        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;
        return new EfCoreUnitOfWork(context).ExecuteSerializableAsync(
            ct => repository.CancelAndReleaseCapacityAsync(
                registrationId,
                expectedOwnerUserId,
                occurrenceId,
                occurredAt,
                EventRegistrationActorProvenance.Attendee,
                expectedOwnerUserId,
                ct));
    }

    private static Task<EventRegistrationTransitionResult> ExecuteUpdateAsync(
        ExploreDbContext context,
        EventRegistrationRepository repository,
        EventRegistration registration)
    {
        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;
        return new EfCoreUnitOfWork(context).ExecuteSerializableAsync(
            ct => repository.UpdateAndAdjustCapacityAsync(
                registration,
                occurrenceId,
                occurredAt,
                EventRegistrationActorProvenance.Organizer,
                null,
                ct));
    }

    private ExploreDbContext CreateRetryingDbContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Persistence integration test retrying cancellation context.");
        return context;
    }

    private static async Task<RegistrationScenario> SeedRegistrationAsync(
        ExploreDbContext context,
        int approvalStatusId = (int)ApprovalStatusEnum.Approved,
        bool includeSecondRegistration = false,
        int sessionMaxAudienceAttendees = 10,
        int? secondApprovalStatusId = null)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = "Registration Cancellation Tenant",
            Slug = "registration-cancel-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Pii = new UserPii
            {
                Email = $"registration-cancel-{Guid.NewGuid():N}@example.com",
                FirstName = "Registration",
                LastName = "Cancel"
            }
        };
        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Pii = new ActorPii { DisplayName = "Registration Cancellation Actor" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = user.Id
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var eventId = Guid.NewGuid();
        var @event = new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Registration Cancellation Event",
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

        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Title = "Registration Cancellation Session",
            TenantId = tenant.Id,
            Tenant = null!,
            MaxAudienceAttendees = sessionMaxAudienceAttendees,
            CurrentAudienceAttendees = 1
        };
        session.Reschedule(
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
            "UTC",
            new EventScheduleProjectionCalculator());
        context.EventSessions.Add(session);

        var intent = new EventRegistrationIntent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            UserId = user.Id,
            User = null!,
            RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
            RegistrationScope = null!,
            ApprovalStatusId = approvalStatusId,
            TenantId = tenant.Id,
            Tenant = null!
        };
        context.EventRegistrationIntents.Add(intent);

        var registration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            UserId = user.Id,
            User = null!,
            EventSessionId = session.Id,
            EventSession = null!,
            EventRegistrationIntentId = intent.Id,
            EventRegistrationIntent = null,
            TenantId = tenant.Id,
            Tenant = null!,
            ApprovalStatusId = approvalStatusId
        };
        context.EventRegistrations.Add(registration);

        EventRegistration? secondRegistration = null;
        if (includeSecondRegistration)
        {
            var secondSession = new EventSession
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Event = null!,
                Title = "Registration Cancellation Second Session",
                TenantId = tenant.Id,
                Tenant = null!,
                MaxAudienceAttendees = 10,
                CurrentAudienceAttendees = 1
            };
            secondSession.Reschedule(
                new DateTimeOffset(2026, 8, 1, 11, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
                "UTC",
                new EventScheduleProjectionCalculator());
            context.EventSessions.Add(secondSession);

            secondRegistration = new EventRegistration
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Event = null!,
                UserId = user.Id,
                User = null!,
                EventSessionId = secondSession.Id,
                EventSession = null!,
                EventRegistrationIntentId = intent.Id,
                EventRegistrationIntent = null,
                TenantId = tenant.Id,
                Tenant = null!,
                ApprovalStatusId = secondApprovalStatusId ?? approvalStatusId
            };
            context.EventRegistrations.Add(secondRegistration);
        }

        await context.SaveChangesAsync();

        return new RegistrationScenario(
            eventId,
            intent.Id,
            registration.Id,
            session.Id,
            user.Id,
            secondRegistration?.Id,
            secondRegistration?.EventSessionId);
    }

    private sealed record RegistrationScenario(
        Guid EventId,
        Guid IntentId,
        Guid RegistrationId,
        Guid SessionId,
        Guid UserId,
        Guid? SecondRegistrationId,
        Guid? SecondSessionId);
}
