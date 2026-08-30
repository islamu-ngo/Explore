// ABOUTME: Defines entity-first persistence for target-scoped legal document aggregates.
// ABOUTME: Requires trusted scope coordinates on every mutable or published read.

namespace Explore.Application.Contracts.Persistence;

using Explore.Domain;

public interface ILegalDocumentRepository
{
    Task<LegalDocument?> GetForUpdateAsync(
        LegalDocumentScope scope,
        Guid? tenantId,
        LegalDocumentKind kind,
        CancellationToken cancellationToken);

    Task<LegalDocument?> GetByIdForUpdateAsync(
        Guid legalDocumentId,
        LegalDocumentScope scope,
        Guid? tenantId,
        CancellationToken cancellationToken);

    Task<LegalDocument?> GetPublishedAsync(
        LegalDocumentScope scope,
        Guid? tenantId,
        LegalDocumentKind kind,
        CancellationToken cancellationToken);

    Task AddAsync(
        LegalDocument legalDocument,
        CancellationToken cancellationToken);
}
