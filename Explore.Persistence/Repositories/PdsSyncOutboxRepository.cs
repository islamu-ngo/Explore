// ABOUTME: Repository implementation for PDS sync outbox operations.
// ABOUTME: Provides efficient queries for background worker polling and atomic outbox management.

using Explore.Application.Contracts.Persistence;
using Explore.Domain.Federation;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class PdsSyncOutboxRepository : GenericRepository<PdsSyncOutbox, Guid>, IPdsSyncOutboxRepository
{
    private readonly ExploreDbContext _dbContext;

    public PdsSyncOutboxRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<PdsSyncOutbox> Create(PdsSyncOutbox outbox)
    {
        await _dbContext.PdsSyncOutbox.AddAsync(outbox);
        await _dbContext.SaveChangesAsync();
        return outbox;
    }

    public async Task<List<PdsSyncOutbox>> GetPendingBatch(int batchSize)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.PdsSyncOutbox
            .Where(o => o.Status == PdsSyncStatus.Pending &&
                        (o.NextRetryAt == null || o.NextRetryAt <= now))
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public new async Task<PdsSyncOutbox?> GetById(Guid id)
    {
        return await _dbContext.PdsSyncOutbox.FindAsync(id);
    }

    public async Task<List<PdsSyncOutbox>> GetBySourceEntity(string sourceEntityType, Guid sourceEntityId)
    {
        return await _dbContext.PdsSyncOutbox
            .Where(o => o.SourceEntityType == sourceEntityType && o.SourceEntityId == sourceEntityId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public new async Task<PdsSyncOutbox> Update(PdsSyncOutbox outbox)
    {
        _dbContext.Entry(outbox).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
        return outbox;
    }

    public async Task<bool> TryMarkAsProcessing(Guid id)
    {
        // Use optimistic concurrency to prevent duplicate processing
        var rowsAffected = await _dbContext.PdsSyncOutbox
            .Where(o => o.Id == id && o.Status == PdsSyncStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, PdsSyncStatus.Processing));

        return rowsAffected > 0;
    }

    public async Task MarkAsCompleted(Guid id, string? uri = null, string? cid = null)
    {
        await _dbContext.PdsSyncOutbox
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, PdsSyncStatus.Completed)
                .SetProperty(o => o.ProcessedAt, DateTime.UtcNow)
                .SetProperty(o => o.LastError, (string?)null));
    }

    public async Task MarkAsFailed(Guid id, string error, bool isRetryable, int retryDelaySeconds, int maxRetries)
    {
        var entry = await _dbContext.PdsSyncOutbox.FindAsync(id);
        if (entry == null) return;

        entry.RetryCount++;
        entry.LastError = error.Length > 2000 ? error[..2000] : error;

        if (!isRetryable || entry.RetryCount >= maxRetries)
        {
            // Permanent failure - mark as failed
            entry.Status = PdsSyncStatus.Failed;
            entry.NextRetryAt = null;
        }
        else
        {
            // Retryable failure - schedule next retry
            entry.Status = PdsSyncStatus.Pending;
            entry.NextRetryAt = DateTime.UtcNow.AddSeconds(retryDelaySeconds);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<PdsSyncOutbox>> GetFailedEntries(int limit = 100)
    {
        return await _dbContext.PdsSyncOutbox
            .Where(o => o.Status == PdsSyncStatus.Failed)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> DeleteCompletedOlderThan(DateTime olderThan)
    {
        return await _dbContext.PdsSyncOutbox
            .Where(o => o.Status == PdsSyncStatus.Completed && o.ProcessedAt < olderThan)
            .ExecuteDeleteAsync();
    }
}
