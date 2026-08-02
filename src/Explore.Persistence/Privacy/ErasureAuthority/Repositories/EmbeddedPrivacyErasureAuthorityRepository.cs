// ABOUTME: Appends and replays retained privacy-erasure facts in the dedicated embedded SQLite authority.
// ABOUTME: Serializes allocation, preserves idempotency, and uses short-lived factory contexts.

using System.Data;
using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Explore.Persistence.Privacy.ErasureAuthority.Repositories;

public sealed class EmbeddedPrivacyErasureAuthorityRepository(
    IDbContextFactory<EmbeddedPrivacyErasureAuthorityDbContext> contextFactory,
    EmbeddedPrivacyErasureAuthorityStorage storage,
    TimeProvider timeProvider,
    IOptions<PrivacyErasureOptions> options) : IPrivacyErasureAuthority, IDisposable
{
    public const int MaximumReadBatchSize = 500;
    private readonly SemaphoreSlim _writerLock = new(1, 1);

    public async Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        await storage.EnsureReadyAsync(cancellationToken);
        await _writerLock.WaitAsync(cancellationToken);
        try
        {
            await using EmbeddedPrivacyErasureAuthorityDbContext db =
                await contextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction =
                await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            PrivacyErasureIntent? existing = await db.ErasureIntents
                .SingleOrDefaultAsync(item => item.IntentId == intent.IntentId, cancellationToken);
            if (existing is not null)
            {
                EnsureSamePayload(existing, intent);
                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO authority_counter (singleton, last_sequence) VALUES (1, 0) ON CONFLICT(singleton) DO NOTHING;",
                cancellationToken);
            PrivacyErasureCounter counter = await db.AuthorityCounters.SingleAsync(cancellationToken);
            long sequence = counter.AllocateNext();
            DateTime recordedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            var fact = PrivacyErasureIntent.Record(
                intent.IntentId,
                sequence,
                intent.SubjectKind,
                intent.SubjectId,
                intent.ReasonCode,
                intent.PolicyVersion,
                recordedAtUtc,
                recordedAtUtc,
                recordedAtUtc + options.Value.AuthorityRetention);
            db.ErasureIntents.Add(fact);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            storage.HardenCompanionFiles();
            return fact;
        }
        finally
        {
            _writerLock.Release();
        }
    }

    public async Task<IReadOnlyList<PrivacyErasureIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(authoritySequence);
        if (limit is < 1 or > MaximumReadBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        await storage.EnsureReadyAsync(cancellationToken);
        await using EmbeddedPrivacyErasureAuthorityDbContext db =
            await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ErasureIntents
            .AsNoTracking()
            .Where(item => item.AuthoritySequence > authoritySequence)
            .OrderBy(item => item.AuthoritySequence)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public void Dispose() => _writerLock.Dispose();

    private static void EnsureSamePayload(
        PrivacyErasureIntent existing,
        PrivacyErasureRequest requested)
    {
        if (existing.SubjectKind != requested.SubjectKind
            || existing.SubjectId != requested.SubjectId
            || existing.ReasonCode != requested.ReasonCode
            || existing.PolicyVersion != requested.PolicyVersion)
        {
            throw new InvalidOperationException(
                "The erasure authority rejected the append payload for this IntentId.");
        }
    }
}
