// ABOUTME: Persists privacy-erasure fences, receipt hashes, and completed policy coverage.
// ABOUTME: Keeps mutable saga state tracked while using bounded queries for receipt cleanup.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class PrivacyErasureStateRepository(ExploreDbContext dbContext)
    : IPrivacyErasureStateRepository
{
    public Task<PrivacyErasureSaga?> GetBySubjectAsync(
        Guid subjectId,
        CancellationToken cancellationToken) =>
        dbContext.PrivacyErasureSagas.SingleOrDefaultAsync(
            item => item.SubjectId == subjectId,
            cancellationToken);

    public Task<PrivacyErasureSaga?> GetByIntentAsync(
        Guid intentId,
        CancellationToken cancellationToken) =>
        dbContext.PrivacyErasureSagas.SingleOrDefaultAsync(
            item => item.IntentId == intentId,
            cancellationToken);

    public Task<PrivacyErasureSaga?> FindByReceiptHashAsync(
        byte[] receiptHash,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receiptHash);
        return dbContext.PrivacyErasureSagas
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.ReceiptHash == receiptHash, cancellationToken);
    }

    public Task<bool> HasCoverageAsync(
        Guid intentId,
        int policyVersion,
        CancellationToken cancellationToken) =>
        dbContext.PrivacyErasurePolicyCoverage
            .AsNoTracking()
            .AnyAsync(
                item => item.IntentId == intentId && item.PolicyVersion == policyVersion,
                cancellationToken);

    public async Task AddSagaAsync(PrivacyErasureSaga saga, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(saga);
        await dbContext.PrivacyErasureSagas.AddAsync(saga, cancellationToken);
    }

    public async Task AddCoverageAsync(
        PrivacyErasurePolicyCoverage coverage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(coverage);
        await dbContext.PrivacyErasurePolicyCoverage.AddAsync(coverage, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
