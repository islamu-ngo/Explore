// ABOUTME: Shared behavioral contract executed against every supported real primary database provider.
// ABOUTME: Covers runtime persistence semantics and Data Protection key survival across provider recreation.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Projections;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Database;

[RequiresStructuredPrimaryDatabase]
[NotInParallel("PrimaryDatabaseProviderBehaviorContract")]
public sealed class PrimaryDatabaseRuntimeSmokeTests
{
    [Test]
    public async Task StructuredRuntimeCredentialsReachBothMigratedSchemas()
    {
        var fixture = PrimaryDatabaseProviderBehaviorFixture.Create();
        await using var applicationContext = fixture.CreateSystemContext();
        await using var dataProtectionContext = fixture.CreateDataProtectionContext();

        await Assert.That(await applicationContext.Database.CanConnectAsync()).IsTrue();
        await applicationContext.SystemSettings.AsNoTracking().Take(1).ToListAsync();
        await dataProtectionContext.DataProtectionKeys.AsNoTracking().Take(1).ToListAsync();
    }
}

[RequiresStructuredPrimaryDatabase]
[NotInParallel("PrimaryDatabaseProviderBehaviorContract")]
public sealed class PrimaryDatabaseProviderBehaviorContractTests
{
    [Test]
    public async Task MigratedProviderSupportsSharedPersistenceBehavior()
    {
        var fixture = PrimaryDatabaseProviderBehaviorFixture.Create();
        await fixture.PrepareAsync();
        var scope = await SeedTenantGraphAsync(fixture);

        await VerifyProjectionLockContentionAsync(fixture);
        await VerifyCrudPagingTenantIsolationAndSoftDeleteAsync(fixture, scope);
        await VerifyOptimisticConcurrencyAsync(fixture, scope);
        await VerifyTransactionsOutboxAndIdempotentReplayAsync(fixture);
    }

    [Test]
    public async Task DataProtectionKeyRingSurvivesProviderRecreation()
    {
        var fixture = PrimaryDatabaseProviderBehaviorFixture.Create();
        var applicationName = $"islamu-event-provider-contract-{Guid.CreateVersion7():N}";
        const string purpose = "provider-restart";
        const string payload = "authenticated-session-ticket";

        string protectedPayload;
        await using (var firstProvider = fixture.BuildDataProtectionProvider(applicationName))
        {
            protectedPayload = firstProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(purpose)
                .Protect(payload);

            await using var scope = firstProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();
            await Assert.That(await context.DataProtectionKeys.CountAsync()).IsGreaterThanOrEqualTo(1);
        }

        await using (var restartedProvider = fixture.BuildDataProtectionProvider(applicationName))
        {
            var restored = restartedProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(purpose)
                .Unprotect(protectedPayload);

            await Assert.That(restored).IsEqualTo(payload);
        }
    }

    private static async Task VerifyProjectionLockContentionAsync(
        PrimaryDatabaseProviderBehaviorFixture fixture)
    {
        await using var owner = fixture.CreateSystemContext();
        await using var contender = fixture.CreateSystemContext();
        await using var transaction = await owner.Database.BeginTransactionAsync();
        Guid tenantId = Guid.CreateVersion7();

        bool acquired = await ProjectionInfrastructure.TryAcquireAdvisoryLockAsync(
            owner,
            projectionLockKey: 82001,
            tenantId,
            exclusive: true,
            CancellationToken.None);
        bool blocked = await ProjectionInfrastructure.TryAcquireAdvisoryLockAsync(
            contender,
            projectionLockKey: 82001,
            tenantId,
            exclusive: false,
            CancellationToken.None);

        await Assert.That(acquired).IsTrue();
        await Assert.That(blocked).IsFalse();
        await transaction.RollbackAsync();

        bool acquiredAfterRelease = await ProjectionInfrastructure.TryAcquireAdvisoryLockAsync(
            contender,
            projectionLockKey: 82001,
            tenantId,
            exclusive: false,
            CancellationToken.None);
        await Assert.That(acquiredAfterRelease).IsTrue();
    }

    private static async Task<ProviderScope> SeedTenantGraphAsync(
        PrimaryDatabaseProviderBehaviorFixture fixture)
    {
        await using var context = fixture.CreateSystemContext();
        var suffix = Guid.CreateVersion7().ToString("N");
        var tenantA = NewTenant($"provider-a-{suffix}");
        var tenantB = NewTenant($"provider-b-{suffix}");
        var locationA = NewLocation(tenantA, "Brussels");
        var locationB = NewLocation(tenantB, "Antwerp");
        var roomA1 = NewRoom(tenantA, locationA, "Room 10", 10);
        var roomA2 = NewRoom(tenantA, locationA, "Room 20", 20);
        var roomA3 = NewRoom(tenantA, locationA, "Room 30", 30);
        var roomB = NewRoom(tenantB, locationB, "Other Tenant Room", 10);

        context.AddRange(tenantA, tenantB, locationA, locationB, roomA1, roomA2, roomA3, roomB);
        await context.SaveChangesAsync();

        return new ProviderScope(
            tenantA.Id,
            tenantB.Id,
            roomA1.Id,
            roomA2.Id,
            roomA3.Id,
            roomB.Id);
    }

    private static async Task VerifyCrudPagingTenantIsolationAndSoftDeleteAsync(
        PrimaryDatabaseProviderBehaviorFixture fixture,
        ProviderScope scope)
    {
        await using (var tenantAContext = fixture.CreateTenantContext(scope.TenantAId))
        {
            var firstPage = await tenantAContext.LocationRooms
                .AsNoTracking()
                .OrderBy(room => room.SortOrder)
                .ThenBy(room => room.Id)
                .Take(2)
                .Select(room => room.Id)
                .ToListAsync();

            await Assert.That(firstPage).Count().IsEqualTo(2);
            await Assert.That(firstPage[0]).IsEqualTo(scope.RoomA1Id);
            await Assert.That(firstPage[1]).IsEqualTo(scope.RoomA2Id);

            var room = await tenantAContext.LocationRooms.SingleAsync(candidate => candidate.Id == scope.RoomA1Id);
            room.Name = "Updated Room";
            await tenantAContext.SaveChangesAsync();

            var deletedRoom = await tenantAContext.LocationRooms.SingleAsync(candidate => candidate.Id == scope.RoomA2Id);
            tenantAContext.Remove(deletedRoom);
            await tenantAContext.SaveChangesAsync();
        }

        await using (var tenantAContext = fixture.CreateTenantContext(scope.TenantAId))
        {
            var visible = await tenantAContext.LocationRooms
                .AsNoTracking()
                .OrderBy(room => room.SortOrder)
                .Select(room => new { room.Id, room.Name })
                .ToListAsync();
            var includingDeleted = await tenantAContext.LocationRooms
                .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
                .AsNoTracking()
                .Where(room => room.Id == scope.RoomA2Id)
                .SingleAsync();

            await Assert.That(visible).Count().IsEqualTo(2);
            await Assert.That(visible[0].Id).IsEqualTo(scope.RoomA1Id);
            await Assert.That(visible[1].Id).IsEqualTo(scope.RoomA3Id);
            await Assert.That(visible[0].Name).IsEqualTo("Updated Room");
            await Assert.That(includingDeleted.IsDeleted).IsTrue();
        }

        await using (var tenantBContext = fixture.CreateTenantContext(scope.TenantBId))
        {
            var visible = await tenantBContext.LocationRooms.AsNoTracking().Select(room => room.Id).ToListAsync();
            await Assert.That(visible).IsEquivalentTo([scope.RoomBId]);
        }

        await using (var missingTenantContext = fixture.CreateTenantContext(null))
        {
            Guid[] contractRoomIds = [scope.RoomA1Id, scope.RoomA2Id, scope.RoomA3Id, scope.RoomBId];
            await Assert.That(await missingTenantContext.LocationRooms
                .AsNoTracking()
                .AnyAsync(room => contractRoomIds.Contains(room.Id))).IsFalse();
        }
    }

    private static async Task VerifyOptimisticConcurrencyAsync(
        PrimaryDatabaseProviderBehaviorFixture fixture,
        ProviderScope scope)
    {
        await using var staleContext = fixture.CreateTenantContext(scope.TenantAId);
        await using var winnerContext = fixture.CreateTenantContext(scope.TenantAId);
        var stale = await staleContext.LocationRooms.SingleAsync(room => room.Id == scope.RoomA3Id);
        var winner = await winnerContext.LocationRooms.SingleAsync(room => room.Id == scope.RoomA3Id);

        winner.Name = "Concurrent Winner";
        await winnerContext.SaveChangesAsync();
        stale.Name = "Stale Writer";

        DbUpdateConcurrencyException? conflict = null;
        try
        {
            await staleContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            conflict = exception;
        }

        await Assert.That(conflict).IsNotNull();
    }

    private static async Task VerifyTransactionsOutboxAndIdempotentReplayAsync(
        PrimaryDatabaseProviderBehaviorFixture fixture)
    {
        var suffix = Guid.CreateVersion7().ToString("N");
        var committedSettingKey = $"provider-contract.commit.{suffix}";
        var rolledBackSettingKey = $"provider-contract.rollback.{suffix}";
        var committedOutbox = NewOutboxMessage(suffix, "committed");
        var rolledBackOutbox = NewOutboxMessage(suffix, "rolled-back");

        await using (var context = fixture.CreateSystemContext())
        {
            var unitOfWork = new EfCoreUnitOfWork(context);
            await unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
            {
                context.SystemSettings.Add(new SystemSetting { SettingKey = committedSettingKey, Value = "committed" });
                context.OutboxMessages.Add(committedOutbox);
                await context.SaveChangesAsync(cancellationToken);
            });
        }

        await using (var context = fixture.CreateSystemContext())
        {
            var unitOfWork = new EfCoreUnitOfWork(context);
            var rollback = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
                {
                    context.SystemSettings.Add(new SystemSetting { SettingKey = rolledBackSettingKey, Value = "rollback" });
                    context.OutboxMessages.Add(rolledBackOutbox);
                    await context.SaveChangesAsync(cancellationToken);
                    throw new InvalidOperationException("rollback contract");
                }));
            await Assert.That(rollback!.Message).IsEqualTo("rollback contract");
        }

        await using (var context = fixture.CreateSystemContext())
        {
            await Assert.That(await context.SystemSettings.AnyAsync(setting => setting.SettingKey == committedSettingKey)).IsTrue();
            await Assert.That(await context.OutboxMessages.AnyAsync(message => message.Id == committedOutbox.Id)).IsTrue();
            await Assert.That(await context.SystemSettings.AnyAsync(setting => setting.SettingKey == rolledBackSettingKey)).IsFalse();
            await Assert.That(await context.OutboxMessages.AnyAsync(message => message.Id == rolledBackOutbox.Id)).IsFalse();

            var repository = new OutboxRepository(context);
            var claim = await repository.TryClaimForProcessing(committedOutbox.Id, DateTime.UtcNow);
            await Assert.That(claim).IsNotNull();
            await Assert.That(await repository.MarkAsCompleted(committedOutbox.Id, claim!.Value)).IsTrue();
            await Assert.That(await repository.MarkAsCompleted(committedOutbox.Id, claim.Value)).IsFalse();
        }

        var tenantId = Guid.CreateVersion7();
        var idempotencyKey = $"provider-contract-{suffix}";
        var now = DateTime.UtcNow;
        var ownerRecord = NewIdempotencyRecord(idempotencyKey, tenantId, now);
        await using (var context = fixture.CreateSystemContext())
        {
            var repository = new IdempotencyRepository(context);
            var claim = await repository.TryClaimAsync(ownerRecord);
            await Assert.That(claim.IsOwner).IsTrue();
            await Assert.That(await repository.CompleteAsync(ownerRecord.Id, 201, "{\"result\":\"created\"}", "application/json")).IsTrue();
        }

        await using (var context = fixture.CreateSystemContext())
        {
            var repository = new IdempotencyRepository(context);
            var replay = await repository.TryClaimAsync(NewIdempotencyRecord(idempotencyKey, tenantId, now.AddSeconds(1)));
            var persisted = await repository.FindAsync(idempotencyKey, tenantId);

            await Assert.That(replay.IsOwner).IsFalse();
            await Assert.That(replay.Record.Id).IsEqualTo(ownerRecord.Id);
            await Assert.That(persisted).IsNotNull();
            await Assert.That(persisted!.StatusCode).IsEqualTo(201);
            await Assert.That(persisted.ResponseBody).IsEqualTo("{\"result\":\"created\"}");
        }
    }

    private static Tenant NewTenant(string slug) => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = slug,
        Slug = slug,
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!,
    };

    private static Location NewLocation(Tenant tenant, string city) => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = $"{city} Venue",
        Country = "BE",
        City = city,
        TenantId = tenant.Id,
        Tenant = tenant,
    };

    private static LocationRoom NewRoom(Tenant tenant, Location location, string name, int sortOrder) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenant.Id,
        Tenant = tenant,
        LocationId = location.Id,
        Location = location,
        Name = name,
        SortOrder = sortOrder,
    };

    private static OutboxMessage NewOutboxMessage(string suffix, string state) => new()
    {
        Id = Guid.CreateVersion7(),
        AggregateType = "ProviderContract",
        AggregateId = Guid.CreateVersion7(),
        EventType = $"provider-contract-{state}-{suffix}",
        Status = OutboxMessageStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        MaxRetries = 3,
    };

    private static IdempotencyRecord NewIdempotencyRecord(string key, Guid tenantId, DateTime createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Key = key,
        TenantId = tenantId,
        UserId = "provider-contract",
        RequestMethod = "POST",
        RequestTarget = "/provider-contract",
        RequestBodyHash = "provider-contract-body",
        PrincipalFingerprint = "provider-contract-principal",
        StatusCode = IdempotencyRecord.InProgressStatusCode,
        CreatedAt = createdAt,
        ExpiresAt = createdAt.AddHours(1),
    };

    private sealed record ProviderScope(
        Guid TenantAId,
        Guid TenantBId,
        Guid RoomA1Id,
        Guid RoomA2Id,
        Guid RoomA3Id,
        Guid RoomBId);
}
