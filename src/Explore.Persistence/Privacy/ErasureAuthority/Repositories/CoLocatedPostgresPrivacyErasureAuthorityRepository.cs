// ABOUTME: Appends and replays co-located PostgreSQL privacy-erasure authority facts.
// ABOUTME: Uses the primary database transaction boundary and a locked monotonic counter row.

using System.Data;
using Explore.Application.Configuration;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Explore.Persistence.Privacy.ErasureAuthority.Repositories;

public sealed class CoLocatedPostgresPrivacyErasureAuthorityRepository(
    CoLocatedPrivacyErasureAuthorityDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<PrivacyErasureOptions> options) : IPrivacyErasureAuthority
{
    public const int MaximumReadBatchSize = 500;

    public async Task<PrivacyErasureIntent> AppendAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        string counterTable = GetCounterTable();
        await dbContext.Database.ExecuteSqlRawAsync(
            $"INSERT INTO {counterTable} (singleton, last_sequence) VALUES (true, 0) ON CONFLICT(singleton) DO NOTHING;",
            cancellationToken);
        PrivacyErasureCounter counter = await dbContext.AuthorityCounters
            .FromSqlRaw($"SELECT * FROM {counterTable} FOR UPDATE")
            .SingleAsync(cancellationToken);
        PrivacyErasureIntent? existing = await dbContext.ErasureIntents
            .SingleOrDefaultAsync(item => item.IntentId == intent.IntentId, cancellationToken);
        if (existing is not null)
        {
            EnsureSamePayload(existing, intent);
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }

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
        dbContext.ErasureIntents.Add(fact);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return fact;
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

        return await dbContext.ErasureIntents
            .AsNoTracking()
            .Where(item => item.AuthoritySequence > authoritySequence)
            .OrderBy(item => item.AuthoritySequence)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private string GetCounterTable()
    {
        IEntityType entityType = dbContext.Model.FindEntityType(typeof(PrivacyErasureCounter))!;
        ISqlGenerationHelper sql = dbContext.GetService<ISqlGenerationHelper>();
        return sql.DelimitIdentifier(entityType.GetTableName()!, entityType.GetSchema());
    }

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
