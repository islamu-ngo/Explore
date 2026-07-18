// ABOUTME: PostgreSQL integration tests for fenced AT Protocol outbox settlement and atomic Jetstream cursor application.
// ABOUTME: Covers stale-worker rollback, idempotent replay, UUID allocation, and tenant presentation isolation.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Event.Persistence.IntegrationTests.Federation;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class AtprotoFederationPersistenceTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task JetstreamApply_AllocatesIdentityAndAdvancesDuplicateReplayWithoutDuplication()
    {
        await fixture.ResetAsync();
        var scope = await SeedScopeAsync("jetstream-replay");
        await using var context = fixture.CreateDbContext();
        var repository = new AtprotoJetstreamRepository(context);
        var now = Utc(10);
        var claim = await repository.TryClaimAsync(
            "wss://jetstream.example/subscribe",
            "worker-a",
            now,
            TimeSpan.FromMinutes(5)) ?? throw new InvalidOperationException("Claim was not acquired.");
        var record = IncomingRecord(sourceVersion: 1, now);

        var applied = await repository.TryApplyAndAdvanceAsync(new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 0,
            NextCursor: 1,
            record,
            [new AtprotoRecordTenantPresentation { TenantId = scope.TenantId, IsVisible = true }],
            Quarantine: null,
            now));
        var replayed = await repository.TryApplyAndAdvanceAsync(new AtprotoJetstreamApplyRequest(
            claim,
            ExpectedCursor: 1,
            NextCursor: 2,
            IncomingRecord(sourceVersion: 1, now.AddSeconds(1)),
            [],
            Quarantine: null,
            now.AddSeconds(1)));

        context.ChangeTracker.Clear();
        var persistedRecord = await context.AtprotoRecords.AsNoTracking().SingleAsync();
        var presentation = await context.AtprotoRecordTenantPresentations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync();
        var state = await context.AtprotoJetstreamConsumerStates.AsNoTracking().SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(replayed).IsTrue();
        await Assert.That(persistedRecord.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(presentation.AtprotoRecordId).IsEqualTo(persistedRecord.Id);
        await Assert.That(state.Cursor).IsEqualTo(2);
        await Assert.That(await context.AtprotoRecords.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task AtprotoRecordRepository_ExposesOnlyCurrentTenantPresentations()
    {
        await fixture.ResetAsync();
        var tenantA = await SeedScopeAsync("presentation-a");
        var tenantB = await SeedScopeAsync("presentation-b");
        Guid recordId;
        await using (var seedContext = fixture.CreateDbContext())
        {
            var record = IncomingRecord(sourceVersion: 1, Utc(10));
            record.Id = Guid.CreateVersion7();
            recordId = record.Id;
            seedContext.AtprotoRecords.Add(record);
            seedContext.AtprotoRecordTenantPresentations.Add(new AtprotoRecordTenantPresentation
            {
                TenantId = tenantA.TenantId,
                AtprotoRecordId = record.Id,
                IsVisible = true,
                SourceVersion = 1,
                EvaluatedAt = Utc(10)
            });
            await seedContext.SaveChangesAsync();
        }

        await using var contextA = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(tenantA.TenantId));
        await using var contextB = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(tenantB.TenantId));
        var visible = await new AtprotoRecordRepository(contextA).GetById(recordId);
        var hidden = await new AtprotoRecordRepository(contextB).GetById(recordId);
        await Assert.That(visible).IsNotNull();
        await Assert.That(hidden).IsNull();
    }

    [Test]
    public async Task PdsSettlement_ReclaimedFenceRollsBackCanonicalAndOwnershipWrites()
    {
        await fixture.ResetAsync();
        var scope = await SeedScopeAsync("pds-fence");
        var now = Utc(10);
        var outbox = CreateOutbox(scope, now);
        await using (var seedContext = fixture.CreateDbContext())
        {
            seedContext.PdsSyncOutbox.Add(outbox);
            await seedContext.SaveChangesAsync();
        }

        PdsSyncClaim staleClaim;
        await using (var claimContext = fixture.CreateDbContext())
        {
            staleClaim = (await new PdsSyncOutboxRepository(claimContext).ClaimDueAsync(
                1,
                "worker-a",
                now,
                TimeSpan.FromMinutes(1))).Single();
        }

        var interceptor = new ReclaimOnSaveInterceptor(async () =>
        {
            await using var reclaimContext = fixture.CreateDbContext();
            var reclaimed = await new PdsSyncOutboxRepository(reclaimContext).ClaimDueAsync(
                1,
                "worker-b",
                now.AddMinutes(2),
                TimeSpan.FromMinutes(5));
            await Assert.That(reclaimed).HasSingleItem();
        });
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(value => value.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(interceptor)
            .Options;
        await using var staleContext = new ExploreDbContext(options);
        staleContext.EnableTenantFilterBypass("ATProto fenced settlement race test.");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            new PdsSyncOutboxRepository(staleContext).TrySettleAsync(
                staleClaim,
                $"at://{outbox.Did}/{outbox.Collection}/{outbox.RecordKey}",
                "bafyreifenced",
                now.AddSeconds(30)));

        await using var verifyContext = fixture.CreateDbContext();
        var persistedOutbox = await verifyContext.PdsSyncOutbox.IgnoreQueryFilters().AsNoTracking().SingleAsync();
        await Assert.That(persistedOutbox.Status).IsEqualTo(PdsSyncStatus.Processing);
        await Assert.That(persistedOutbox.LeaseOwner).IsEqualTo("worker-b");
        await Assert.That(persistedOutbox.LeaseFence).IsEqualTo(staleClaim.LeaseFence + 1);
        await Assert.That(await verifyContext.AtprotoRecords.CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoOutboundRecordOwnerships.IgnoreQueryFilters().CountAsync()).IsEqualTo(0);
        await Assert.That(await verifyContext.AtprotoRecordTenantPresentations.IgnoreQueryFilters().CountAsync()).IsEqualTo(0);
    }

    private async Task<FederationScope> SeedScopeAsync(string slug)
    {
        await using var context = fixture.CreateDbContext();
        var now = Utc(9);
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = slug,
            Slug = $"{slug}-{Guid.CreateVersion7():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Pii = new UserPii
            {
                Email = $"{slug}@example.test",
                FirstName = "ATProto",
                LastName = "Owner"
            },
            EmailVerified = true,
            ConcurrencyStamp = Guid.CreateVersion7(),
            CreatedAt = now
        };
        var tenantUser = new TenantUser
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = tenant,
            UserId = user.Id,
            User = user,
            StatusId = (int)TenantUserStatusEnum.Active,
            JoinedAt = now,
            CreatedAt = now
        };
        context.TenantUsers.Add(tenantUser);
        await context.SaveChangesAsync();
        return new FederationScope(tenant.Id, user.Id);
    }

    private static PdsSyncOutbox CreateOutbox(FederationScope scope, DateTime createdAt) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = scope.TenantId,
        UserId = scope.UserId,
        Did = "did:plc:fenced-owner",
        Collection = "community.lexicon.calendar.event",
        RecordKey = "3m7fenced",
        Operation = PdsSyncOperation.Create,
        Payload = "{\"name\":\"Fenced event\",\"createdAt\":\"2026-07-18T10:00:00Z\"}",
        PayloadHash = new string('a', 64),
        IdempotencyKey = $"event:{Guid.CreateVersion7():N}:create",
        PdsHost = "https://pds.example",
        SourceEntityType = "Event",
        SourceEntityId = Guid.CreateVersion7(),
        SourceVersion = Guid.CreateVersion7(),
        Status = PdsSyncStatus.Pending,
        CreatedAt = createdAt,
        MaxRetries = 3
    };

    private static AtprotoRecord IncomingRecord(long sourceVersion, DateTime observedAt) => new()
    {
        Did = "did:plc:remote-owner",
        Collection = "community.lexicon.calendar.event",
        RecordKey = "3m7remote",
        Cid = $"bafyreiv{sourceVersion}",
        Uri = "at://did:plc:remote-owner/community.lexicon.calendar.event/3m7remote",
        SourceVersion = sourceVersion,
        RecordJson = "{\"name\":\"Remote event\",\"createdAt\":\"2026-07-18T10:00:00Z\"}",
        RecordHash = new string('b', 64),
        IndexedAt = observedAt,
        UpdatedAt = observedAt
    };

    private static DateTime Utc(int hour) => new(2026, 7, 18, hour, 0, 0, DateTimeKind.Utc);

    private sealed record FederationScope(Guid TenantId, Guid UserId);
    private sealed record StaticTenantContext(Guid TenantId) : ITenantContext;

    private sealed class ReclaimOnSaveInterceptor(Func<Task> reclaim) : SaveChangesInterceptor
    {
        private bool _invoked;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!_invoked)
            {
                _invoked = true;
                await reclaim();
            }

            return result;
        }
    }
}
