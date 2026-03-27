// ABOUTME: Repository implementation for generic outbox message persistence.
// ABOUTME: Uses ExecuteUpdateAsync for atomic optimistic-lock transitions; mirrors PdsSyncOutboxRepository patterns.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class OutboxRepository : GenericRepository<OutboxMessage, Guid>, IOutboxRepository
{
    private readonly ExploreDbContext _dbContext;

    public OutboxRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<OutboxMessage> Create(OutboxMessage message)
    {
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();
        return message;
    }

    public async Task<List<OutboxMessage>> GetPendingBatch(int batchSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Status == OutboxMessageStatus.Pending &&
                        (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task<bool> TryMarkAsProcessing(Guid id, CancellationToken ct = default)
    {
        // Atomic WHERE+SET prevents duplicate processing across concurrent processors
        var rowsAffected = await _dbContext.OutboxMessages
            .Where(m => m.Id == id && m.Status == OutboxMessageStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxMessageStatus.Processing), ct);

        return rowsAffected > 0;
    }

    public async Task MarkAsCompleted(Guid id, CancellationToken ct = default)
    {
        await _dbContext.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxMessageStatus.Completed)
                .SetProperty(m => m.ProcessedAt, DateTime.UtcNow)
                .SetProperty(m => m.LastError, (string?)null), ct);
    }

    public async Task MarkAsFailed(Guid id, string error, bool isRetryable, int retryDelaySeconds, int maxRetries, CancellationToken ct = default)
    {
        var entry = await _dbContext.OutboxMessages.FindAsync([id], ct);
        if (entry == null) return;

        entry.RetryCount++;
        entry.LastError = error.Length > 2000 ? error[..2000] : error;

        if (!isRetryable || entry.RetryCount >= maxRetries)
        {
            entry.Status = entry.RetryCount >= maxRetries
                ? OutboxMessageStatus.DeadLettered
                : OutboxMessageStatus.Failed;
            entry.NextRetryAt = null;
            entry.DeadLetteredAt = entry.Status == OutboxMessageStatus.DeadLettered
                ? DateTime.UtcNow
                : null;
        }
        else
        {
            // Retryable — move back to Pending with a future NextRetryAt
            entry.Status = OutboxMessageStatus.Pending;
            entry.NextRetryAt = DateTime.UtcNow.AddSeconds(retryDelaySeconds);
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<List<OutboxMessage>> GetFailedEntries(int limit = 100, CancellationToken ct = default)
    {
        return await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Status == OutboxMessageStatus.Failed || m.Status == OutboxMessageStatus.DeadLettered)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> DeleteCompletedOlderThan(DateTime cutoff, CancellationToken ct = default)
    {
        return await _dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Completed && m.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
