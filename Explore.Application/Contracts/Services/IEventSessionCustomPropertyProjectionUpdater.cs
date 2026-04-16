// ABOUTME: Transactional updater contract for the atomic event-session custom-property projection read model.
// ABOUTME: Writers call UpdateFor*/RemoveFor* inside command handler transactions; operators call Rebuild/Drain for recovery.

namespace Explore.Application.Contracts.Services;

public interface IEventSessionCustomPropertyProjectionUpdater
{
    const string ProjectionName = "event_session_custom_property_projection";
    const int ProjectionVersion = 1;

    Task UpdateForValueAsync(Guid valueId, CancellationToken cancellationToken);

    Task UpdateForDefinitionAsync(Guid definitionId, CancellationToken cancellationToken);

    Task RemoveForDefinitionAsync(Guid definitionId, CancellationToken cancellationToken);

    Task RefreshForEventSessionAsync(Guid eventSessionId, CancellationToken cancellationToken);

    Task<ProjectionRebuildResult> RebuildForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<int> DrainDirtyScopesForTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
