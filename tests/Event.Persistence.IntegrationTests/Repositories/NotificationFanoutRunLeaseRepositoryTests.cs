// ABOUTME: PostgreSQL integration tests for fenced notification fanout run leases and checkpoints.
// ABOUTME: Proves contention, expiry recovery, tenant bounds, stale cursor rejection, and crash replay.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Repositories;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class NotificationFanoutRunLeaseRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task TryClaimOccurrenceAsync_TwoWorkers_ProducesOneActiveFence()
    {
        await fixture.ResetAsync();
        FanoutScenario scenario = await SeedOccurrenceAsync("fanout-claim-contention");
        DateTime claimedAt = Utc(2026, 8, 1, 12);

        await using var contextA = fixture.CreateDbContext();
        await using var contextB = fixture.CreateDbContext();
        var repositoryA = new NotificationFanoutRunRepository(contextA);
        var repositoryB = new NotificationFanoutRunRepository(contextB);

        NotificationFanoutClaim?[] claims = await Task.WhenAll(
            repositoryA.TryClaimOccurrenceAsync(
                scenario.TenantId,
                scenario.OccurrenceId,
                "worker-a",
                claimedAt,
                TimeSpan.FromMinutes(1),
                CancellationToken.None),
            repositoryB.TryClaimOccurrenceAsync(
                scenario.TenantId,
                scenario.OccurrenceId,
                "worker-b",
                claimedAt,
                TimeSpan.FromMinutes(1),
                CancellationToken.None));

        await Assert.That(claims.Count(claim => claim is not null)).IsEqualTo(1);
        NotificationFanoutClaim winner = claims.Single(claim => claim is not null)!;
        await Assert.That(winner.TenantId).IsEqualTo(scenario.TenantId);
        await Assert.That(winner.OccurrenceId).IsEqualTo(scenario.OccurrenceId);
        await Assert.That(winner.LeaseToken).IsNotEqualTo(Guid.Empty);
        await Assert.That(winner.Fence).IsGreaterThan(0);
    }

    [Test]
    public async Task LeaseRenewalAndExpiry_PreventEarlyTakeoverThenIssueNewFence()
    {
        await fixture.ResetAsync();
        FanoutScenario scenario = await SeedOccurrenceAsync("fanout-lease-expiry");
        DateTime claimedAt = Utc(2026, 8, 1, 12);

        await using var context = fixture.CreateDbContext();
        var repository = new NotificationFanoutRunRepository(context);
        NotificationFanoutClaim first = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-a",
            claimedAt,
            TimeSpan.FromMinutes(1),
            CancellationToken.None) ?? throw new InvalidOperationException("Initial claim was not acquired.");

        bool renewed = await repository.TryRenewClaimAsync(
            first,
            claimedAt.AddSeconds(30),
            claimedAt.AddMinutes(2),
            CancellationToken.None);
        NotificationFanoutClaim? earlyTakeover = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-b",
            claimedAt.AddSeconds(90),
            TimeSpan.FromMinutes(1),
            CancellationToken.None);
        NotificationFanoutClaim? recovered = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-b",
            claimedAt.AddMinutes(2).AddTicks(1),
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        await Assert.That(renewed).IsTrue();
        await Assert.That(earlyTakeover).IsNull();
        await Assert.That(recovered).IsNotNull();
        await Assert.That(recovered!.LeaseToken).IsNotEqualTo(first.LeaseToken);
        await Assert.That(recovered.Fence).IsGreaterThan(first.Fence);
    }

    [Test]
    public async Task TryCheckpointAsync_RejectsStaleExpectedCursorAndPreservesCounts()
    {
        await fixture.ResetAsync();
        FanoutScenario scenario = await SeedOccurrenceAsync("fanout-checkpoint-stale");
        DateTime claimedAt = Utc(2026, 8, 1, 12);
        var cursor1 = new NotificationFanoutAudienceCursor(claimedAt.AddHours(-2), Guid.CreateVersion7());
        var cursor2 = new NotificationFanoutAudienceCursor(claimedAt.AddHours(-1), Guid.CreateVersion7());

        await using var context = fixture.CreateDbContext();
        var repository = new NotificationFanoutRunRepository(context);
        NotificationFanoutClaim claim = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "checkpoint-worker",
            claimedAt,
            TimeSpan.FromMinutes(5),
            CancellationToken.None) ?? throw new InvalidOperationException("Initial claim was not acquired.");

        bool firstCheckpoint = await repository.TryCheckpointAsync(
            claim,
            expectedCursor: null,
            cursor1,
            processedDelta: 2,
            createdDelta: 2,
            claimedAt.AddSeconds(1),
            CancellationToken.None);
        bool staleCheckpoint = await repository.TryCheckpointAsync(
            claim,
            expectedCursor: null,
            cursor2,
            processedDelta: 100,
            createdDelta: 100,
            claimedAt.AddSeconds(2),
            CancellationToken.None);
        bool nextCheckpoint = await repository.TryCheckpointAsync(
            claim,
            cursor1,
            cursor2,
            processedDelta: 1,
            createdDelta: 1,
            claimedAt.AddSeconds(3),
            CancellationToken.None);
        bool completed = await repository.TryCompleteAsync(
            claim,
            claimedAt.AddSeconds(4),
            CancellationToken.None);
        NotificationFanoutRun? run = await repository.GetByOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            trackChanges: false,
            CancellationToken.None);

        await Assert.That(firstCheckpoint).IsTrue();
        await Assert.That(staleCheckpoint).IsFalse();
        await Assert.That(nextCheckpoint).IsTrue();
        await Assert.That(completed).IsTrue();
        await Assert.That(run).IsNotNull();
        await Assert.That(run!.ProcessedCount).IsEqualTo(3);
        await Assert.That(run.CreatedNotificationCount).IsEqualTo(3);
    }

    [Test]
    public async Task OccurrenceLease_IsTenantBoundAndStaleWorkerCannotCompleteAfterTakeover()
    {
        await fixture.ResetAsync();
        FanoutScenario scenario = await SeedOccurrenceAsync("fanout-tenant-fence");
        DateTime claimedAt = Utc(2026, 8, 1, 12);
        Guid wrongTenantId = Guid.CreateVersion7();

        await using var context = fixture.CreateDbContext();
        var repository = new NotificationFanoutRunRepository(context);
        NotificationFanoutClaim? wrongTenantClaim = await repository.TryClaimOccurrenceAsync(
            wrongTenantId,
            scenario.OccurrenceId,
            "wrong-tenant",
            claimedAt,
            TimeSpan.FromMinutes(1),
            CancellationToken.None);
        NotificationFanoutRun? wrongTenantRun = await repository.GetByOccurrenceAsync(
            wrongTenantId,
            scenario.OccurrenceId,
            trackChanges: false,
            CancellationToken.None);
        NotificationFanoutClaim first = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-a",
            claimedAt,
            TimeSpan.FromMinutes(1),
            CancellationToken.None) ?? throw new InvalidOperationException("Initial claim was not acquired.");
        NotificationFanoutClaim second = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-b",
            claimedAt.AddMinutes(1).AddTicks(1),
            TimeSpan.FromMinutes(1),
            CancellationToken.None) ?? throw new InvalidOperationException("Expired claim was not recovered.");

        bool staleCompletion = await repository.TryCompleteAsync(
            first,
            claimedAt.AddMinutes(1).AddSeconds(1),
            CancellationToken.None);
        bool winnerCompletion = await repository.TryCompleteAsync(
            second,
            claimedAt.AddMinutes(1).AddSeconds(2),
            CancellationToken.None);

        await Assert.That(wrongTenantClaim).IsNull();
        await Assert.That(wrongTenantRun).IsNull();
        await Assert.That(staleCompletion).IsFalse();
        await Assert.That(winnerCompletion).IsTrue();
    }

    [Test]
    public async Task CrashBeforeCheckpoint_ReplaysPreviousCursorWithoutSkippingReadPage()
    {
        await fixture.ResetAsync();
        FanoutScenario scenario = await SeedOccurrenceAsync("fanout-crash-replay");
        DateTime claimedAt = Utc(2026, 8, 1, 12);
        var committedCursor = new NotificationFanoutAudienceCursor(
            claimedAt.AddHours(-2),
            Guid.CreateVersion7());
        var uncommittedPageCursor = new NotificationFanoutAudienceCursor(
            claimedAt.AddHours(-1),
            Guid.CreateVersion7());

        await using var context = fixture.CreateDbContext();
        var repository = new NotificationFanoutRunRepository(context);
        NotificationFanoutClaim first = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-a",
            claimedAt,
            TimeSpan.FromMinutes(1),
            CancellationToken.None) ?? throw new InvalidOperationException("Initial claim was not acquired.");
        bool checkpointed = await repository.TryCheckpointAsync(
            first,
            expectedCursor: null,
            committedCursor,
            processedDelta: 2,
            createdDelta: 2,
            claimedAt.AddSeconds(1),
            CancellationToken.None);

        NotificationFanoutClaim replay = await repository.TryClaimOccurrenceAsync(
            scenario.TenantId,
            scenario.OccurrenceId,
            "worker-b",
            claimedAt.AddMinutes(1).AddTicks(1),
            TimeSpan.FromMinutes(1),
            CancellationToken.None) ?? throw new InvalidOperationException("Expired claim was not recovered.");
        bool replayCheckpointed = await repository.TryCheckpointAsync(
            replay,
            committedCursor,
            uncommittedPageCursor,
            processedDelta: 2,
            createdDelta: 2,
            claimedAt.AddMinutes(1).AddSeconds(1),
            CancellationToken.None);

        await Assert.That(checkpointed).IsTrue();
        await Assert.That(replay.Cursor).IsEqualTo(committedCursor);
        await Assert.That(replayCheckpointed).IsTrue();
    }

    private async Task<FanoutScenario> SeedOccurrenceAsync(string slugPrefix)
    {
        await using var context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = $"Fanout {slugPrefix}",
            Slug = $"{slugPrefix}-{Guid.NewGuid():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var actor = new Actor
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenant.Id,
            Tenant = null!,
            ActorTypeId = (int)ActorTypeEnum.Bot,
            ActorType = null!,
            Pii = new ActorPii { DisplayName = "Fanout source" },
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        var @event = new Explore.Domain.Event
        {
            Id = Guid.CreateVersion7(),
            Title = "Fanout lease event",
            ActorId = actor.Id,
            Actor = null!,
            TenantId = tenant.Id,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            ConcurrencyStamp = Guid.CreateVersion7(),
        };
        context.Events.Add(@event);
        await context.SaveChangesAsync();

        DateTime occurredAt = Utc(2026, 8, 1, 11);
        NotificationFanoutOccurrence occurrence = NotificationFanoutOccurrence.Create(
            Guid.CreateVersion7(),
            tenant.Id,
            @event.Id,
            sessionId: null,
            occurredAt,
            audienceCutoffAt: occurredAt,
            Guid.CreateVersion7(),
            "{\"fields\":[\"startTime\"]}",
            "{\"startTime\":\"2026-08-01T09:00:00Z\"}",
            "{\"startTime\":\"2026-08-01T10:00:00Z\"}",
            "event.updated",
            templateVersion: 1,
            (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
            policyVersion: 1,
            priority: 30,
            notBefore: occurredAt,
            sourceType: "event",
            sourceId: @event.Id,
            coalescingKey: $"event:{@event.Id:N}:schedule",
            coalescingWindowEndsAt: occurredAt);
        context.NotificationFanoutOccurrences.Add(occurrence);
        await context.SaveChangesAsync();
        return new FanoutScenario(tenant.Id, occurrence.Id);
    }

    private static DateTime Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, DateTimeKind.Utc);

    private sealed record FanoutScenario(Guid TenantId, Guid OccurrenceId);
}
