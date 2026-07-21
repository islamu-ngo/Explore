// ABOUTME: Deterministic provider-neutral checks for complete AT Protocol PDS snapshot reconciliation.
// ABOUTME: Covers canonical idempotency, missing-record tombstones, rejected-record invalidation, and cursor isolation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Domain;
using Explore.Domain.Federation;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Event.Persistence.IntegrationTests.Federation;

public sealed class AtprotoPdsSnapshotRepositoryTests
{
    [Test]
    public async Task CompleteSnapshot_ReconcilesCanonicalStateWithoutLocalAggregatesOrCursorMutation()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseInMemoryDatabase($"atproto-snapshot-{Guid.NewGuid():N}")
            .ConfigureWarnings(value => value.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Provider-neutral ATProto snapshot repository test.");
        var repository = new AtprotoJetstreamRepository(context);
        DateTime now = DateTime.UtcNow;
        AtprotoJetstreamClaim claim = await repository.TryClaimAsync(
            "https://jetstream.example",
            "snapshot-worker",
            now,
            TimeSpan.FromMinutes(5)) ?? throw new InvalidOperationException("Claim was not acquired.");
        const string did = "did:plc:snapshot-owner";
        AtprotoRecord missing = Record(did, "missing", 100, now);
        AtprotoRecord rejected = Record(did, "rejected", 100, now);
        context.AtprotoRecords.AddRange(missing, rejected);
        context.AtprotoEventProjections.AddRange(
            Projection(missing, 100, now),
            Projection(rejected, 100, now));
        Guid tenantId = Guid.CreateVersion7();
        context.AtprotoRecordTenantPresentations.AddRange(
            Presentation(tenantId, missing, 100, now),
            Presentation(tenantId, rejected, 100, now));
        await context.SaveChangesAsync();
        AtprotoRecord accepted = Record(did, "accepted", 0, now);
        var snapshot = new AtprotoPdsSnapshot(
            did,
            [
                new("community.lexicon.calendar.event", accepted.RecordKey),
                new("community.lexicon.calendar.event", rejected.RecordKey)
            ],
            [new(accepted, Projection(accepted, 0, now))]);
        var request = new AtprotoPdsSnapshotApplyRequest(
            claim,
            [did],
            [snapshot],
            [tenantId],
            SnapshotVersion: 200,
            ObservedAt: now.AddSeconds(1));

        bool applied = await repository.TryReconcileAsync(request, CancellationToken.None);
        bool replayed = await repository.TryReconcileAsync(request, CancellationToken.None);

        context.ChangeTracker.Clear();
        AtprotoRecord[] records = await context.AtprotoRecords.AsNoTracking()
            .OrderBy(value => value.RecordKey)
            .ToArrayAsync();
        Dictionary<string, AtprotoRecord> byKey = records.ToDictionary(value => value.RecordKey);
        Dictionary<Guid, bool> visibility = await context.AtprotoRecordTenantPresentations
            .IgnoreQueryFilters()
            .ToDictionaryAsync(value => value.AtprotoRecordId, value => value.IsVisible);
        await Assert.That(applied).IsTrue();
        await Assert.That(replayed).IsTrue();
        await Assert.That(records).Count().IsEqualTo(3);
        await Assert.That(byKey[accepted.RecordKey].SourceVersion).IsEqualTo(200);
        await Assert.That(byKey[accepted.RecordKey].SourceCursor).IsNull();
        await Assert.That(byKey[missing.RecordKey].TombstonedAt).IsNotNull();
        await Assert.That(byKey[rejected.RecordKey].TombstonedAt).IsNull();
        await Assert.That(visibility[accepted.Id]).IsTrue();
        await Assert.That(visibility[missing.Id]).IsFalse();
        await Assert.That(visibility[rejected.Id]).IsFalse();
        await Assert.That(await context.AtprotoEventProjections.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.AtprotoJetstreamConsumerStates.Select(value => value.Cursor).SingleAsync())
            .IsEqualTo(0);
        await Assert.That(await context.Events.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.EventRegistrations.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task RejectedPresentRecord_AtEqualSnapshotVersion_PreservesLiveCanonicalProjectionAndPresentation()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseInMemoryDatabase($"atproto-snapshot-equal-version-{Guid.NewGuid():N}")
            .ConfigureWarnings(value => value.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var context = new ExploreDbContext(options);
        context.EnableTenantFilterBypass("Provider-neutral ATProto snapshot repository test.");
        var repository = new AtprotoJetstreamRepository(context);
        DateTime now = DateTime.UtcNow;
        AtprotoJetstreamClaim claim = await repository.TryClaimAsync(
            "https://jetstream.example",
            "snapshot-worker",
            now,
            TimeSpan.FromMinutes(5)) ?? throw new InvalidOperationException("Claim was not acquired.");
        const string did = "did:plc:snapshot-owner";
        const long version = 200;
        AtprotoRecord live = Record(did, "equal-version-live", version, now);
        Guid tenantId = Guid.CreateVersion7();
        context.AtprotoRecords.Add(live);
        context.AtprotoEventProjections.Add(Projection(live, version, now));
        context.AtprotoRecordTenantPresentations.Add(Presentation(tenantId, live, version, now));
        await context.SaveChangesAsync();
        var request = new AtprotoPdsSnapshotApplyRequest(
            claim,
            [did],
            [new AtprotoPdsSnapshot(
                did,
                [new("community.lexicon.calendar.event", live.RecordKey)],
                [])],
            [tenantId],
            SnapshotVersion: version,
            ObservedAt: now.AddSeconds(1));

        bool applied = await repository.TryReconcileAsync(request, CancellationToken.None);

        context.ChangeTracker.Clear();
        AtprotoRecord persisted = await context.AtprotoRecords.AsNoTracking().SingleAsync();
        AtprotoRecordTenantPresentation presentation = await context.AtprotoRecordTenantPresentations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync();
        await Assert.That(applied).IsTrue();
        await Assert.That(persisted.SourceVersion).IsEqualTo(version);
        await Assert.That(persisted.SourceCursor).IsEqualTo(version);
        await Assert.That(persisted.TombstonedAt).IsNull();
        await Assert.That(await context.AtprotoEventProjections.CountAsync()).IsEqualTo(1);
        await Assert.That(presentation.IsVisible).IsTrue();
    }

    private static AtprotoRecord Record(string did, string key, long version, DateTime observedAt) => new()
    {
        Id = Guid.CreateVersion7(),
        Did = did,
        Collection = "community.lexicon.calendar.event",
        RecordKey = key,
        Cid = $"bafy-{key}",
        Uri = $"at://{did}/community.lexicon.calendar.event/{key}",
        Direction = AtprotoRecordDirection.Inbound,
        Provenance = AtprotoRecordProvenance.Jetstream,
        SourceVersion = version,
        SourceCursor = version,
        RecordJson = $"{{\"name\":\"{key}\"}}",
        RecordHash = new string('a', 64),
        IndexedAt = observedAt,
        UpdatedAt = observedAt
    };

    private static AtprotoEventProjection Projection(
        AtprotoRecord record,
        long version,
        DateTime observedAt) => new()
    {
        AtprotoRecordId = record.Id,
        Name = record.RecordKey,
        CreatedAt = new DateTimeOffset(observedAt),
        SourceVersion = version,
        MaterializedAt = observedAt
    };

    private static AtprotoRecordTenantPresentation Presentation(
        Guid tenantId,
        AtprotoRecord record,
        long version,
        DateTime observedAt) => new()
        {
            TenantId = tenantId,
            AtprotoRecordId = record.Id,
            IsVisible = true,
            SourceVersion = version,
            EvaluatedAt = observedAt
        };
}
