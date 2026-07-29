// ABOUTME: Proves PostgreSQL ticketing row locks serialize pool deletion against assignment.
// ABOUTME: Uses explicit task gates so the losing assignment cannot create a dangling reference.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
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
        await using ExploreDbContext assignmentContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        await using ExploreDbContext deletionContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var assignmentRepository = new EventTicketCatalogRepository(assignmentContext);
        var deletionRepository = new EventTicketCatalogRepository(deletionContext);
        var assignmentUow = new EfCoreUnitOfWork(assignmentContext);
        var deletionUow = new EfCoreUnitOfWork(deletionContext);
        var draftLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var poolLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<bool> assignment = assignmentUow.ExecuteInTransactionAsync(async token =>
        {
            EventTicketCatalogVersion? draft = await assignmentRepository.GetDraftCatalogForUpdateAsync(eventId, tenantId, token);
            await Assert.That(draft).IsNotNull();
            draftLocked.SetResult();
            await poolLocked.Task.WaitAsync(token);

            EventCapacityPool? pool = await assignmentRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            return pool is not null;
        });

        await draftLocked.Task;
        bool deleted = await deletionUow.ExecuteInTransactionAsync(async token =>
        {
            EventCapacityPool? pool = await deletionRepository.GetActiveCapacityPoolForUpdateAsync(poolId, eventId, tenantId, token);
            await Assert.That(pool).IsNotNull();
            poolLocked.SetResult();
            await Assert.That(await deletionRepository.HasLiveTicketTypeReferencesAsync(poolId, eventId, tenantId, token)).IsFalse();
            pool!.Delete(DateTime.UtcNow, Guid.NewGuid());
            await deletionRepository.UpdateCapacityPoolAsync(pool, token);
            return true;
        });

        await Assert.That(deleted).IsTrue();
        await Assert.That(await assignment).IsFalse();

        await using ExploreDbContext verifyContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verifyRepository = new EventTicketCatalogRepository(verifyContext);
        await Assert.That(await verifyContext.EventCapacityPools.AnyAsync(pool => pool.Id == poolId)).IsFalse();
        await Assert.That(await verifyRepository.HasLiveTicketTypeReferencesAsync(poolId, eventId, tenantId, CancellationToken.None)).IsFalse();
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
            TenantId = tenant.Id, Tenant = tenant, VisibilityTypeId = 1, VisibilityType = null!, EventStatusId = 1, EventStatus = null!, EventFormatId = 1, EventFormat = null!, TotalViews = 0
        };
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenant.Id, eventId, "USD", 1);
        EventCapacityPool pool = EventCapacityPool.Create(tenant.Id, eventId, "Pool", 10, 900, CapacityOversellPolicyEnum.Disallow, true);
        context.AddRange(eventTarget, catalog, pool);
        await context.SaveChangesAsync();
        return (tenant.Id, eventId, pool.Id);
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
