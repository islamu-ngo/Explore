// ABOUTME: Integration tests for EfCoreUnitOfWork transactional correctness against a real Postgres database.
// ABOUTME: Covers: rollback on failure, commit on success, nested transaction guard, generic overload.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Microsoft.EntityFrameworkCore;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.UnitOfWork;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
public class EfCoreUnitOfWorkTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public EfCoreUnitOfWorkTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenOperationSucceeds_CommitsAllWrites()
    {
        using var context = _fixture.CreateDbContext();
        var uow = new EfCoreUnitOfWork(context);
        var key1 = $"uow-commit-{Guid.NewGuid():N}";
        var key2 = $"uow-commit-{Guid.NewGuid():N}";

        await uow.ExecuteInTransactionAsync(async ct =>
        {
            context.Set<SystemSetting>().Add(new SystemSetting { SettingKey = key1, Value = "v1" });
            await context.SaveChangesAsync(ct);
            context.Set<SystemSetting>().Add(new SystemSetting { SettingKey = key2, Value = "v2" });
            await context.SaveChangesAsync(ct);
        });

        // Verify both records persisted
        using var verifyContext = _fixture.CreateDbContext();
        var s1 = await verifyContext.Set<SystemSetting>().FirstOrDefaultAsync(s => s.SettingKey == key1);
        var s2 = await verifyContext.Set<SystemSetting>().FirstOrDefaultAsync(s => s.SettingKey == key2);

        await Assert.That(s1).IsNotNull();
        await Assert.That(s2).IsNotNull();
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackAllWrites()
    {
        using var context = _fixture.CreateDbContext();
        var uow = new EfCoreUnitOfWork(context);
        var key = $"uow-rollback-{Guid.NewGuid():N}";

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await uow.ExecuteInTransactionAsync(async ct =>
            {
                context.Set<SystemSetting>().Add(new SystemSetting { SettingKey = key, Value = "should-not-persist" });
                await context.SaveChangesAsync(ct);

                // Simulate mid-workflow failure after first write
                throw new InvalidOperationException("Simulated workflow failure");
            });
        });

        // Verify the partial write was rolled back
        using var verifyContext = _fixture.CreateDbContext();
        var setting = await verifyContext.Set<SystemSetting>().FirstOrDefaultAsync(s => s.SettingKey == key);

        await Assert.That(setting).IsNull();
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Generic_WhenOperationSucceeds_ReturnsValue()
    {
        using var context = _fixture.CreateDbContext();
        var uow = new EfCoreUnitOfWork(context);
        var key = $"uow-generic-{Guid.NewGuid():N}";

        var result = await uow.ExecuteInTransactionAsync(async ct =>
        {
            var setting = new SystemSetting { SettingKey = key, Value = "generic-return" };
            context.Set<SystemSetting>().Add(setting);
            await context.SaveChangesAsync(ct);
            return setting.SettingKey;
        });

        await Assert.That(result).IsEqualTo(key);

        // Verify record was persisted
        using var verifyContext = _fixture.CreateDbContext();
        var persisted = await verifyContext.Set<SystemSetting>().FirstOrDefaultAsync(s => s.SettingKey == key);
        await Assert.That(persisted).IsNotNull();
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenNestedTransaction_ThrowsInvalidOperationException()
    {
        using var context = _fixture.CreateDbContext();
        var uow = new EfCoreUnitOfWork(context);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await uow.ExecuteInTransactionAsync(async ct =>
            {
                // Attempt to nest — must throw immediately
                await uow.ExecuteInTransactionAsync(_ => Task.CompletedTask, ct);
            });
        });
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenNestedTransaction_ErrorMessageMentionsNestedTransactions()
    {
        using var context = _fixture.CreateDbContext();
        var uow = new EfCoreUnitOfWork(context);
        InvalidOperationException? caught = null;

        try
        {
            await uow.ExecuteInTransactionAsync(async ct =>
            {
                await uow.ExecuteInTransactionAsync(_ => Task.CompletedTask, ct);
            });
        }
        catch (InvalidOperationException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Message).Contains("Nested transactions are not supported");
    }

    [Test]
    public async Task ExecuteInTransactionAsync_WhenConcurrencyStampStale_ThrowsConcurrencyConflictException()
    {
        var tenantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        // Seed a real concurrency-aware aggregate so stale writes trigger EF concurrency translation.
        using (var seedContext = _fixture.CreateDbContext())
        {
            seedContext.Set<Tenant>().Add(new Tenant
            {
                Id = tenantId,
                FullName = $"Tenant {tenantId:N}",
                Slug = $"tenant-{tenantId:N}",
                TenantStatusId = (int)TenantStatusEnum.Active,
                TenantStatus = null!
            });

            seedContext.Set<Location>().Add(new Location
            {
                Id = locationId,
                FullName = "Concurrency Test Location",
                Country = "BE",
                City = "Brussels",
                Pii = new LocationPii
                {
                    Address = "123 Test Street",
                    Postcode = "1000"
                },
                TenantId = tenantId,
                Tenant = null!
            });

            seedContext.Set<LocationRoom>().Add(new LocationRoom
            {
                Id = roomId,
                Name = "Room A",
                LocationId = locationId,
                Location = null!,
                TenantId = tenantId,
                Tenant = null!
            });

            await seedContext.SaveChangesAsync();
        }

        // Load the row in session A.
        using var contextA = _fixture.CreateDbContext();
        var entityA = await contextA.Set<LocationRoom>().FirstAsync(room => room.Id == roomId);

        // Mutate and commit the same row from session B so its stamp advances.
        using (var contextB = _fixture.CreateDbContext())
        {
            var entityB = await contextB.Set<LocationRoom>().FirstAsync(room => room.Id == roomId);
            entityB.Name = "Room B";
            await contextB.SaveChangesAsync();
        }

        // Session A still holds the old stamp; the save must be translated.
        var uowA = new EfCoreUnitOfWork(contextA);
        ConcurrencyConflictException? caught = null;
        try
        {
            await uowA.ExecuteInTransactionAsync(async ct =>
            {
                entityA.Name = "Room C";
                await contextA.SaveChangesAsync(ct);
            });
        }
        catch (ConcurrencyConflictException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(caught.EntityType).IsEqualTo(nameof(LocationRoom));
    }
}
