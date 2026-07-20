// ABOUTME: Executes global account erasure with a ledger in the application database.
// ABOUTME: Commits ledger, PII mutation, audit, checkpoint, and correction outbox atomically before cache invalidation.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Domain;

namespace Explore.Application.Services;

public sealed class ApplicationDatabasePrivacyErasureWorkflow(
    IUserRepository userRepository,
    IPrivacyErasureLedgerRepository ledgerRepository,
    IUnitOfWork unitOfWork,
    PrivacyErasureApplier applier) : IPrivacyErasureService
{
    public async Task EraseUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty id is required.", nameof(userId));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (await userRepository.GetById(userId) is null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        PrivacyErasureRequest request = PrivacyErasureRequest.Create(
            Guid.CreateVersion7(),
            PrivacyErasureSubjectKind.User,
            userId,
            PrivacyErasureReasonCode.AccountDeletion,
            1);
        PrivacyErasureApplier.AppliedErasure applied =
            await unitOfWork.ExecuteSerializableAsync(async ct =>
            {
                PrivacyErasureIntent fact = await ledgerRepository.AppendAsync(request, ct);
                PrivacyErasureApplier.PreparedErasure prepared = await applier.PrepareAsync(fact, ct);
                return await applier.ApplyInCurrentTransactionAsync(fact, prepared, ct);
            }, cancellationToken);
        await applier.InvalidateAfterCommitAsync(applied);
    }

    public Task ReplayPendingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
