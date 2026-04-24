// ABOUTME: Contract for transactional event template sync apply orchestration using an operator-selected plan.
// ABOUTME: The service re-diffs server-side, enforces quotas/concurrency, updates projections, and writes audit entries.

using Explore.Application.DTOs.EventTemplateSync;

namespace Explore.Application.Contracts.Services;

public interface IEventTemplateSyncService
{
    Task<TemplateSyncOutcomeDto> ApplySyncAsync(
        Guid eventId,
        TemplateSyncPlanDto plan,
        int baseProvenanceVersion,
        CancellationToken cancellationToken);
}
