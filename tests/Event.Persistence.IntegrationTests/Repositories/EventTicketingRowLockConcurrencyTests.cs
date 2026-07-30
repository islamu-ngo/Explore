// ABOUTME: Proves PostgreSQL ticketing row locks in both deletion-winning and assignment-winning races.
// ABOUTME: Uses real draft ticket mutations, live-reference guards, and explicit task gates without arbitrary delays.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventTicketingRowLockConcurrencyTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task DeleteWinningPoolLock_LeavesNoLiveTicketTypeReferencingDeletedPool()
    {
        (Guid tenantId, Guid eventId, Guid poolId) = await SeedAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        CancellationToken cancellationToken = timeout.Token;
        await using ExploreDbContext assignmentContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        await using ExploreDbContext deletionContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var assignmentRepository = new EventTicketCatalogRepository(assignmentContext);
        var deletionRepository = new EventTicketCatalogRepository(deletionContext);
        var assignmentUow = new EfCoreUnitOfWork(assignmentContext);
        var deletionUow = new EfCoreUnitOfWork(deletionContext);
        var deletionPoolLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deletionMayCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var competingAssignmentStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> deletion = deletionUow.ExecuteInTransactionAsync(async token =>
        {
            EventCapacityPool? pool = await deletionRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            await Assert.That(pool).IsNotNull();
            deletionPoolLocked.TrySetResult();
            await deletionMayCommit.Task.WaitAsync(token);
            await Assert.That(await deletionRepository.HasLiveTicketTypeReferencesAsync(poolId, eventId, tenantId, token)).IsFalse();
            pool!.Delete(DateTime.UtcNow, Guid.NewGuid());
            await deletionRepository.UpdateCapacityPoolAsync(pool, token);
            return true;
        }, cancellationToken);

        Task? competingAssignment = null;
        bool deleted = false;
        Exception? testFailure = null;
        try
        {
            await deletionPoolLocked.Task.WaitAsync(cancellationToken);
            competingAssignment = assignmentUow.ExecuteInTransactionAsync(async token =>
            {
                EventTicketCatalogVersion? draft = await assignmentRepository.GetDraftCatalogForUpdateAsync(eventId, tenantId, token);
                await Assert.That(draft).IsNotNull();
                await assignmentContext.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '250ms'", token);
                competingAssignmentStarted.TrySetResult();
                await assignmentRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
                throw new InvalidOperationException("The competing assignment unexpectedly acquired the deletion pool lock.");
            }, cancellationToken);

            await competingAssignmentStarted.Task.WaitAsync(cancellationToken);
            PostgresException? lockException = null;
            Exception? competingAssignmentException = null;
            try
            {
                await competingAssignment.WaitAsync(cancellationToken);
            }
            catch (PostgresException exception)
            {
                lockException = exception;
            }
            catch (Exception exception) when (exception.GetBaseException() is PostgresException postgresException)
            {
                lockException = postgresException;
            }
            catch (Exception exception)
            {
                competingAssignmentException = exception;
            }

            if (competingAssignmentException is not null)
            {
                throw competingAssignmentException;
            }

            PostgresException observedLockException = lockException
                ?? throw new InvalidOperationException("The competing assignment did not observe a PostgreSQL lock timeout.");
            await Assert.That(observedLockException.SqlState).IsEqualTo(PostgresErrorCodes.LockNotAvailable);
        }
        catch (Exception exception)
        {
            testFailure = exception;
            throw;
        }
        finally
        {
            deletionMayCommit.TrySetResult();
            if (competingAssignment is not null)
            {
                await competingAssignment.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            if (testFailure is null)
            {
                deleted = await deletion;
            }
            else
            {
                await ((Task)deletion).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }

        await Assert.That(deleted).IsTrue();

        await using ExploreDbContext retryAssignmentContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var retryAssignmentRepository = new EventTicketCatalogRepository(retryAssignmentContext);
        var retryAssignmentUow = new EfCoreUnitOfWork(retryAssignmentContext);
        Guid? retriedTicketId = await retryAssignmentUow.ExecuteInTransactionAsync(async token =>
        {
            EventTicketCatalogVersion? draft = await retryAssignmentRepository.GetDraftCatalogForUpdateAsync(eventId, tenantId, token);
            await Assert.That(draft).IsNotNull();
            EventCapacityPool? pool = await retryAssignmentRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            return await AssignTicketTypeAsync(retryAssignmentRepository, draft!, pool, tenantId, eventId, token);
        }, cancellationToken);

        await Assert.That(retriedTicketId).IsNull();

        await using ExploreDbContext verifyContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verifyRepository = new EventTicketCatalogRepository(verifyContext);
        await Assert.That(await verifyContext.EventCapacityPools.AnyAsync(pool => pool.Id == poolId)).IsFalse();
        await Assert.That(await verifyRepository.HasLiveTicketTypeReferencesAsync(poolId, eventId, tenantId, CancellationToken.None)).IsFalse();
    }

    [Test]
    public async Task AssignmentWinningPoolLock_PersistsTicketAndRejectsConcurrentPoolDeletion()
    {
        (Guid tenantId, Guid eventId, Guid poolId) = await SeedAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        CancellationToken cancellationToken = timeout.Token;
        await using ExploreDbContext assignmentContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        await using ExploreDbContext deletionContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var assignmentRepository = new EventTicketCatalogRepository(assignmentContext);
        var deletionRepository = new EventTicketCatalogRepository(deletionContext);
        var assignmentUow = new EfCoreUnitOfWork(assignmentContext);
        var deletionUow = new EfCoreUnitOfWork(deletionContext);
        var assignmentPoolLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deletionAttemptStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var assignmentMayCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<Guid?> assignment = assignmentUow.ExecuteInTransactionAsync(async token =>
        {
            EventTicketCatalogVersion? draft = await assignmentRepository.GetDraftCatalogForUpdateAsync(eventId, tenantId, token);
            await Assert.That(draft).IsNotNull();
            EventCapacityPool? pool = await assignmentRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            await Assert.That(pool).IsNotNull();
            assignmentPoolLocked.TrySetResult();
            await assignmentMayCommit.Task.WaitAsync(token);
            return await AssignTicketTypeAsync(assignmentRepository, draft!, pool, tenantId, eventId, token);
        }, cancellationToken);

        await assignmentPoolLocked.Task.WaitAsync(cancellationToken);
        Task competingDeletion = deletionUow.ExecuteInTransactionAsync(async token =>
        {
            deletionAttemptStarted.TrySetResult();
            await deletionContext.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '250ms'", token);
            await deletionRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            throw new InvalidOperationException("The competing deletion unexpectedly acquired the assignment pool lock.");
        }, cancellationToken);

        await deletionAttemptStarted.Task.WaitAsync(cancellationToken);
        PostgresException? lockException = null;
        Exception? competingDeletionException = null;
        try
        {
            await competingDeletion.WaitAsync(cancellationToken);
        }
        catch (PostgresException exception)
        {
            lockException = exception;
        }
        catch (Exception exception) when (exception.GetBaseException() is PostgresException postgresException)
        {
            lockException = postgresException;
        }
        catch (Exception exception)
        {
            competingDeletionException = exception;
        }
        finally
        {
            assignmentMayCommit.TrySetResult();
        }

        Guid assignedTicketId = (await assignment.WaitAsync(cancellationToken))
            ?? throw new InvalidOperationException("The assignment transaction did not persist a ticket type.");
        if (competingDeletionException is not null)
        {
            throw competingDeletionException;
        }

        PostgresException observedLockException = lockException
            ?? throw new InvalidOperationException("The competing deletion did not observe a PostgreSQL lock timeout.");
        await Assert.That(observedLockException.SqlState).IsEqualTo(PostgresErrorCodes.LockNotAvailable);

        await using ExploreDbContext retryDeletionContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var retryDeletionRepository = new EventTicketCatalogRepository(retryDeletionContext);
        var retryDeletionUow = new EfCoreUnitOfWork(retryDeletionContext);
        (bool deleted, bool poolRemainedActive, bool hasLiveReferences) = await retryDeletionUow.ExecuteInTransactionAsync(async token =>
        {
            EventCapacityPool? pool = await retryDeletionRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            if (pool is null)
            {
                return (false, false, false);
            }

            bool hasReferences = await retryDeletionRepository.HasLiveTicketTypeReferencesAsync(poolId, eventId, tenantId, token);
            if (hasReferences)
            {
                return (false, true, true);
            }

            pool.Delete(DateTime.UtcNow, Guid.NewGuid());
            await retryDeletionRepository.UpdateCapacityPoolAsync(pool, token);
            return (true, true, false);
        }, cancellationToken);

        await Assert.That(deleted).IsFalse();
        await Assert.That(poolRemainedActive).IsTrue();
        await Assert.That(hasLiveReferences).IsTrue();

        await using ExploreDbContext verifyContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verifyRepository = new EventTicketCatalogRepository(verifyContext);
        EventTicketType? persistedTicket = await verifyRepository.GetTicketTypeByIdEventAndTenantAsync(
            assignedTicketId,
            eventId,
            tenantId,
            CancellationToken.None);
        await Assert.That(persistedTicket).IsNotNull();
        await Assert.That(persistedTicket!.CapacityPoolId).IsEqualTo(poolId);
        await Assert.That(await verifyRepository.HasLiveTicketTypeReferencesAsync(poolId, eventId, tenantId, CancellationToken.None)).IsTrue();
        await Assert.That(await verifyRepository.GetCapacityPoolByIdEventAndTenantAsync(poolId, eventId, tenantId, CancellationToken.None)).IsNotNull();
    }

    private static async Task<Guid?> AssignTicketTypeAsync(
        EventTicketCatalogRepository repository,
        EventTicketCatalogVersion catalog,
        EventCapacityPool? pool,
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (pool is null)
        {
            return null;
        }

        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(),
            tenantId,
            catalog.Id,
            "Assigned ticket",
            catalog.CurrencyCode,
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            pool.Id,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, tenantId, eventId, 1));
        await repository.UpdateAsync(catalog, cancellationToken);
        return ticket.Id;
    }

    private async Task<(Guid TenantId, Guid EventId, Guid PoolId)> SeedAsync()
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context = fixture.CreateDbContext();
        TenantStatus activeStatus = await context.TenantStatuses.SingleAsync(status => status.Id == (int)TenantStatusEnum.Active);
        var tenant = new Tenant { FullName = "Ticketing lock tenant", Slug = $"ticket-lock-{Guid.NewGuid():N}", TenantStatusId = activeStatus.Id, TenantStatus = activeStatus };
        context.Tenants.Add(tenant);
        var user = new User { Pii = new UserPii { Email = "ticket-lock@example.test", FirstName = "Ticket", LastName = "Lock" } };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var actor = new Actor { Pii = new ActorPii { DisplayName = "Ticket Lock Actor" }, ActorTypeId = 1, ActorType = null!, UserId = user.Id };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        Guid eventId = Guid.CreateVersion7();
        var eventTarget = new DomainEvent
        {
            Id = eventId, Title = "Ticket lock event", Subtitle = "", Description = "", FirstSessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), LastSessionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            EventTypeId = 1, AudienceGenderId = 1, AudienceAgeId = 1, ActorId = actor.Id, Actor = null!, OrganizerActorId = actor.Id,
            TenantId = tenant.Id, Tenant = tenant, VisibilityTypeId = 1, VisibilityType = null!, EventStatusId = 1, EventStatus = null!, EventFormatId = 1, EventFormat = null!, EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated, TotalViews = 0
        };
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenant.Id, eventId, "USD", 1);
        EventCapacityPool pool = EventCapacityPool.Create(tenant.Id, eventId, "Pool", 10, 900, CapacityHoldPolicyEnum.TimedHoldOnSelection, CapacityOversellPolicyEnum.Disallow, true);
        context.AddRange(eventTarget, catalog, pool);
        await context.SaveChangesAsync();
        return (tenant.Id, eventId, pool.Id);
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
