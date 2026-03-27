// ABOUTME: Repository interface for idempotency key persistence.
// ABOUTME: Supports lookup by composite key (Key + TenantId) and save for new records.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId, CancellationToken cancellationToken = default);
    Task SaveAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
