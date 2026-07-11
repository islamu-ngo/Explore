// ABOUTME: Contract for transactional event-session template sync apply orchestration using an operator-selected plan.
// ABOUTME: The service re-diffs server-side, enforces quotas/concurrency, updates projections, and writes audit entries.

using Explore.Application.DTOs.EventSessionTemplateSync;

namespace Explore.Application.Contracts.Services;

public interface IEventSessionTemplateSyncService
{
    Task<TemplateSyncOutcomeDto> ApplySyncAsync(
        Guid eventSessionId,
        TemplateSyncPlanDto plan,
        int baseProvenanceVersion,
        CancellationToken cancellationToken);
}
