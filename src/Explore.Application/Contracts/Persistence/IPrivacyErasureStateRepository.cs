// ABOUTME: Defines entity-first persistence for privacy-erasure fences, sagas, and policy coverage.
// ABOUTME: Keeps receipt lookup and policy-version state inside the application database boundary.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IPrivacyErasureStateRepository
{
    Task<PrivacyErasureSaga?> GetBySubjectAsync(Guid subjectId, CancellationToken cancellationToken);
    Task<PrivacyErasureSaga?> GetByIntentAsync(Guid intentId, CancellationToken cancellationToken);
    Task<PrivacyErasureSaga?> FindByReceiptHashAsync(byte[] receiptHash, CancellationToken cancellationToken);
    Task<bool> HasCoverageAsync(Guid intentId, int policyVersion, CancellationToken cancellationToken);
    Task AddSagaAsync(PrivacyErasureSaga saga, CancellationToken cancellationToken);
    Task AddCoverageAsync(PrivacyErasurePolicyCoverage coverage, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
