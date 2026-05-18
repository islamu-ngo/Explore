// ABOUTME: Repository contract for tenant-owned typed settings documents.
// ABOUTME: Supports additive typed JSONB resolution without changing legacy scalar setting repositories.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain.Settings.Documents;

public interface ITenantSettingsDocumentRepository : IGenericRepository<TenantSettingsDocument, Guid>
{
    Task<TenantSettingsDocument?> GetByTenantAndDocumentKey(
        Guid tenantId,
        string documentKey,
        CancellationToken cancellationToken = default);

    Task<TenantSettingsDocument?> GetTrackedByTenantAndDocumentKey(
        Guid tenantId,
        string documentKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantSettingsDocument>> GetManyForTenant(
        Guid tenantId,
        IEnumerable<string> documentKeys,
        CancellationToken cancellationToken = default);
}
