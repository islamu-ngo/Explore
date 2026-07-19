// ABOUTME: Atomically records consumed ATProto bootstrap jtis in the shared idempotency table.
// ABOUTME: Uses PostgreSQL conflict handling so concurrent API instances permit exactly one winner.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class AtprotoBootstrapReplayRepository(ExploreDbContext dbContext) : IAtprotoBootstrapReplayRepository
{
    private const string KeyPrefix = "atproto-bootstrap:";

    public async Task<bool> TryConsumeAsync(
        string jti,
        Guid tenantId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti) || jti.Length > 64 || tenantId == Guid.Empty)
        {
            return false;
        }

        var key = KeyPrefix + jti;
        var now = DateTime.UtcNow;
        if (expiresAt <= now)
        {
            return false;
        }

        if (!dbContext.Database.IsRelational())
        {
            if (await dbContext.IdempotencyRecords.AnyAsync(
                    record => record.Key == key && record.TenantId == tenantId,
                    cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            dbContext.IdempotencyRecords.Add(CreateRecord(key, tenantId, now, expiresAt.UtcDateTime));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        var id = Guid.CreateVersion7();
        var expiresAtUtc = expiresAt.UtcDateTime;
        var inserted = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO idempotency_records
                (id, key, tenant_id, request_method, request_target, request_body_hash,
                 principal_fingerprint, status_code, created_at, expires_at)
            VALUES
                ({id}, {key}, {tenantId}, {"POST"}, {"/api/auth/atproto/session"}, {string.Empty},
                 {string.Empty}, {0}, {now}, {expiresAtUtc})
            ON CONFLICT (key, tenant_id) DO NOTHING
            """, cancellationToken).ConfigureAwait(false);
        return inserted == 1;
    }

    private static IdempotencyRecord CreateRecord(
        string key,
        Guid tenantId,
        DateTime createdAt,
        DateTime expiresAt) => new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            TenantId = tenantId,
            RequestMethod = HttpMethod.Post.Method,
            RequestTarget = "/api/auth/atproto/session",
            RequestBodyHash = string.Empty,
            PrincipalFingerprint = string.Empty,
            StatusCode = 0,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        };
}
