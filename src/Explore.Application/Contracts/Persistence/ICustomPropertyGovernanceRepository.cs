// ABOUTME: Repository contract for the Rule 12 governance report aggregation queries.
// ABOUTME: Aggregates across event and event-session runtime definitions for promotion analysis.

using Explore.Domain.Enums;

namespace Explore.Application.Contracts.Persistence;

public record GovernanceDefinitionRow(
    Guid TenantId,
    string Namespace,
    string Key,
    string DisplayName,
    string EntityScope,
    PropertyType PropertyType,
    ExposureLevel ExposureLevel,
    bool IsSearchable,
    bool IsFilterable,
    bool IsExportable,
    bool IsModerationRelevant,
    bool IsAnalyticsRelevant,
    bool IsSystemOwned,
    int ActiveInstanceCount,
    DateTime? LastUsedAt);

public interface ICustomPropertyGovernanceRepository
{
    Task<(List<GovernanceDefinitionRow> Items, int TotalCount)> GetGovernanceRowsAsync(
        Guid tenantId,
        string? entityScopeFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> GetTotalEventCountForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken);
}
