// ABOUTME: Persistence contract for retained OrganizationTenant legitimacy evidence.
// ABOUTME: Supports tenant-scoped submission replay, review, and safe detail projections.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IOrganizationTenantEvidenceRepository
    : IGenericRepository<OrganizationTenantEvidence, Guid>
{
    Task<OrganizationTenantEvidence?> GetDetailsAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken);

    Task<OrganizationTenantEvidence?> GetByDocumentAsync(
        Guid organizationTenantId,
        Guid documentStorageObjectId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrganizationTenantEvidence>> ListByParticipationAsync(
        Guid organizationTenantId,
        CancellationToken cancellationToken);
}
