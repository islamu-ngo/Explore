// ABOUTME: Repository implementation for idempotency replay persistence using ExploreDbContext.
// ABOUTME: Claims keys atomically by tenant and completes responses only from the claim owner.

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

    public async Task<IdempotencyClaim> TryClaimAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsRelational())
        {
            var claimed = await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO idempotency_records
                    (id, key, tenant_id, user_id, request_method, request_target, request_content_type,
                     request_body_hash, principal_fingerprint, status_code, response_body, content_type,
                     created_at, expires_at)
                VALUES
                    ({record.Id}, {record.Key}, {record.TenantId}, {record.UserId}, {record.RequestMethod},
                     {record.RequestTarget}, {record.RequestContentType}, {record.RequestBodyHash},
                     {record.PrincipalFingerprint}, {record.StatusCode}, {record.ResponseBody},
                     {record.ContentType}, {record.CreatedAt}, {record.ExpiresAt})
                ON CONFLICT (key, tenant_id) DO UPDATE SET
                    id = EXCLUDED.id,
                    user_id = EXCLUDED.user_id,
                    request_method = EXCLUDED.request_method,
                    request_target = EXCLUDED.request_target,
                    request_content_type = EXCLUDED.request_content_type,
                    request_body_hash = EXCLUDED.request_body_hash,
                    principal_fingerprint = EXCLUDED.principal_fingerprint,
                    status_code = EXCLUDED.status_code,
                    response_body = EXCLUDED.response_body,
                    content_type = EXCLUDED.content_type,
                    created_at = EXCLUDED.created_at,
                    expires_at = EXCLUDED.expires_at
                WHERE idempotency_records.expires_at <= {record.CreatedAt}
                """, cancellationToken);

            if (claimed == 1)
            {
                return new IdempotencyClaim(record, IsOwner: true);
            }
        }
        else
        {
            var current = await FindAsync(record.Key, record.TenantId, cancellationToken);
            if (current is null)
            {
                await SaveAsync(record, cancellationToken);
                return new IdempotencyClaim(record, IsOwner: true);
            }

            return new IdempotencyClaim(current, IsOwner: false);
        }

        var existing = await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstAsync(
                current => current.Key == record.Key && current.TenantId == record.TenantId,
                cancellationToken);
        return new IdempotencyClaim(existing, IsOwner: false);
    }

    public async Task<bool> CompleteAsync(
        Guid recordId,
        int statusCode,
        string? responseBody,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var record = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(
                candidate => candidate.Id == recordId
                    && candidate.StatusCode == IdempotencyRecord.InProgressStatusCode,
                cancellationToken);
            if (record is null)
            {
                return false;
            }

            record.StatusCode = statusCode;
            record.ResponseBody = responseBody;
            record.ContentType = contentType;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var updated = await _dbContext.IdempotencyRecords
            .Where(record => record.Id == recordId && record.StatusCode == IdempotencyRecord.InProgressStatusCode)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(record => record.StatusCode, statusCode)
                    .SetProperty(record => record.ResponseBody, responseBody)
                    .SetProperty(record => record.ContentType, contentType),
                cancellationToken);
        return updated == 1;
    }

    public async Task<bool> ReleaseAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var record = await _dbContext.IdempotencyRecords.FirstOrDefaultAsync(
                candidate => candidate.Id == recordId
                    && candidate.StatusCode == IdempotencyRecord.InProgressStatusCode,
                cancellationToken);
            if (record is null)
            {
                return false;
            }

            _dbContext.IdempotencyRecords.Remove(record);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var deleted = await _dbContext.IdempotencyRecords
            .Where(record => record.Id == recordId && record.StatusCode == IdempotencyRecord.InProgressStatusCode)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted == 1;
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
