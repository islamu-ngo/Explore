// ABOUTME: Appends and reads PII-free erasure facts inside the application DbContext transaction.
// ABOUTME: Allocates contiguous sequences and rejects idempotency-key payload conflicts without exposing mutations.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Privacy.ErasureAuthority.Repositories;

public sealed class ApplicationDatabasePrivacyErasureLedgerRepository(
    ExploreDbContext dbContext,
    TimeProvider timeProvider) : IPrivacyErasureLedgerRepository
{
    public async Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        PrivacyErasureCounter counter = await GetCounterForUpdateAsync(cancellationToken);
        PrivacyErasureIntent? existing = await FindAsync(intent.IntentId, cancellationToken);
        if (existing is not null)
        {
            EnsurePayloadMatches(existing, intent);
            return existing;
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        PrivacyErasureIntent fact = PrivacyErasureIntent.Record(
            intent.IntentId,
            counter.AllocateNext(),
            intent.SubjectKind,
            intent.SubjectId,
            intent.ReasonCode,
            intent.PolicyVersion,
            now,
            now);
        dbContext.PrivacyErasureIntents.Add(fact);
        await dbContext.SaveChangesAsync(cancellationToken);
        return fact;
    }

    public async Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureIntent intent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        PrivacyErasureCounter counter = await GetCounterForUpdateAsync(cancellationToken);
        PrivacyErasureIntent? existing = await FindAsync(intent.IntentId, cancellationToken);
        if (existing is not null)
        {
            EnsurePayloadMatches(existing, intent);
            if (existing.AuthoritySequence != intent.AuthoritySequence)
            {
                throw new InvalidOperationException("The erasure intent already occupies a different sequence.");
            }

            return existing;
        }

        counter.AdvanceTo(intent.AuthoritySequence);
        dbContext.PrivacyErasureIntents.Add(intent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return intent;
    }

    public async Task<IReadOnlyList<PrivacyErasureIntent>> ReadAfterAsync(
        long authoritySequence,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(authoritySequence);
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return await dbContext.PrivacyErasureIntents
            .AsNoTracking()
            .Where(item => item.AuthoritySequence > authoritySequence)
            .OrderBy(item => item.AuthoritySequence)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private Task<PrivacyErasureIntent?> FindAsync(
        Guid intentId,
        CancellationToken cancellationToken) =>
        dbContext.PrivacyErasureIntents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.IntentId == intentId, cancellationToken);

    private async Task<PrivacyErasureCounter> GetCounterForUpdateAsync(
        CancellationToken cancellationToken)
    {
        PrivacyErasureCounter? counter;
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO privacy_erasure_authority.authority_counter (singleton, last_sequence) VALUES (TRUE, 0) ON CONFLICT (singleton) DO NOTHING",
                cancellationToken);
            counter = await dbContext.PrivacyErasureCounters
                .FromSqlRaw(
                    "SELECT singleton, last_sequence FROM privacy_erasure_authority.authority_counter WHERE singleton FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
        }
        else
        {
            counter = await dbContext.PrivacyErasureCounters
                .SingleOrDefaultAsync(cancellationToken);
        }

        if (counter is not null)
        {
            return counter;
        }

        counter = PrivacyErasureCounter.Start();
        dbContext.PrivacyErasureCounters.Add(counter);
        return counter;
    }

    private static void EnsurePayloadMatches(
        PrivacyErasureIntent existing,
        PrivacyErasureRequest incoming)
    {
        if (existing.SubjectKind != incoming.SubjectKind
            || existing.SubjectId != incoming.SubjectId
            || existing.ReasonCode != incoming.ReasonCode
            || existing.PolicyVersion != incoming.PolicyVersion)
        {
            throw new InvalidOperationException(
                "The erasure IntentId is already recorded with a different normalized payload.");
        }
    }

    private static void EnsurePayloadMatches(
        PrivacyErasureIntent existing,
        PrivacyErasureIntent incoming)
    {
        if (existing.SubjectKind != incoming.SubjectKind
            || existing.SubjectId != incoming.SubjectId
            || existing.ReasonCode != incoming.ReasonCode
            || existing.PolicyVersion != incoming.PolicyVersion
            || existing.RequestedAtUtc != incoming.RequestedAtUtc
            || existing.RecordedAtUtc != incoming.RecordedAtUtc)
        {
            throw new InvalidOperationException(
                "The erasure IntentId is already recorded with a different normalized payload.");
        }
    }
}
