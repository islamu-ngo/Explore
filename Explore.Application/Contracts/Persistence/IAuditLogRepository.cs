// ABOUTME: Repository contract for AuditLog entity writes used by operator-visible audit trails.
// ABOUTME: Sync workflows use this to persist structured template-sync audit entries without coupling Application to DbContext.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IAuditLogRepository : IGenericRepository<AuditLog, Guid>
{
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetTemplateSyncHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
