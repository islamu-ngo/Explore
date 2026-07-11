// ABOUTME: Repository contract for durable notification fanout run state.
// ABOUTME: Provides idempotent source lookup and worker-polling primitives without exposing EF Core.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface INotificationFanoutRunRepository : IGenericRepository<NotificationFanoutRun, Guid>
{
    Task<NotificationFanoutRun?> GetBySourceAsync(
        Guid tenantId,
        string fanoutKind,
        int notificationEntityTypeId,
        Guid entityId,
        Guid sourceActorId,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);

    Task<List<NotificationFanoutRun>> GetPendingBatchAsync(
        int pageSize,
        CancellationToken cancellationToken = default);
}
