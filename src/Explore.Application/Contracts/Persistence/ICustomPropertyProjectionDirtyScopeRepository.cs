// ABOUTME: Repository contract for the custom-property projection dirty-scope backlog.
// ABOUTME: Inline writers upsert pending rows during rebuild contention; rebuild worker drains on completion.

using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public interface ICustomPropertyProjectionDirtyScopeRepository
{
    Task UpsertAsync(
        string projectionName,
        int projectionVersion,
        Guid tenantId,
        CustomPropertyProjectionScopeType scopeType,
        Guid scopeId,
        Guid? definitionId,
        string reason,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomPropertyProjectionDirtyScope>> GetPendingAsync(
        string projectionName,
        int projectionVersion,
        Guid tenantId,
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkDrainedAsync(
        IReadOnlyCollection<long> ids,
        DateTimeOffset drainedAt,
        CancellationToken cancellationToken);

    Task<int> CountPendingAsync(
        string projectionName,
        int projectionVersion,
        Guid tenantId,
        CancellationToken cancellationToken);
}
