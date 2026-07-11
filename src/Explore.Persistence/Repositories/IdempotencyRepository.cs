// ABOUTME: Repository implementation for idempotency key persistence using ExploreDbContext.
// ABOUTME: FindAsync filters by Key + TenantId and excludes expired records.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly ExploreDbContext _dbContext;

    public IdempotencyRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Key == key && r.TenantId == tenantId && r.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        await _dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountExpiredAsync(
        DateTime expiresBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .Where(record => record.ExpiresAt <= expiresBeforeUtc)
            .OrderBy(record => record.ExpiresAt)
            .Take(batchSize)
            .CountAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(
        DateTime expiresBeforeUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var expiredRecordIds = _dbContext.IdempotencyRecords
            .Where(record => record.ExpiresAt <= expiresBeforeUtc)
            .OrderBy(record => record.ExpiresAt)
            .Select(record => record.Id)
            .Take(batchSize);

        return await _dbContext.IdempotencyRecords
            .Where(record => expiredRecordIds.Contains(record.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
