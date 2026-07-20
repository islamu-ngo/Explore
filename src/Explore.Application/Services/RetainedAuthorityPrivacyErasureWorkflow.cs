// ABOUTME: Executes retained-authority-first platform account erasure and ordered replay.
// ABOUTME: Never falls back after authority failure and mirrors each fact in the application transaction.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.PrivacyErasure;
using Explore.Application.Contracts.Services;
using Explore.Application.Exceptions;
using Explore.Domain;

namespace Explore.Application.Services;

public sealed class RetainedAuthorityPrivacyErasureWorkflow(
    IUserRepository userRepository,
    IPrivacyErasureReplayCheckpointRepository checkpointRepository,
    IPrivacyErasureAuthority authority,
    IUnitOfWork unitOfWork,
    PrivacyErasureApplier applier) : IPrivacyErasureService
{
    private const int ReplayBatchSize = 100;

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
        await AppendWithAmbiguousAcknowledgementRetryAsync(request, cancellationToken);
        await ReplayPendingAsync(cancellationToken);
    }

    public async Task ReplayPendingAsync(CancellationToken cancellationToken)
    {
        PrivacyErasureReplayCheckpoint? latest = await checkpointRepository.GetLatestAsync(cancellationToken);
        if (latest is not null)
        {
            IReadOnlyList<PrivacyErasureIntent> evidence =
                await authority.ReadAfterAsync(latest.AuthoritySequence - 1, 1, cancellationToken);
            if (evidence.Count != 1 || !latest.Matches(evidence[0]))
            {
                throw new InvalidOperationException(
                    "The application erasure checkpoint is not continuous with the retained authority.");
            }

            await applier.InvalidateRetainedIntentAsync(evidence[0], cancellationToken);
        }

        long afterSequence = latest?.AuthoritySequence ?? 0;
        while (true)
        {
            IReadOnlyList<PrivacyErasureIntent> pending =
                await authority.ReadAfterAsync(afterSequence, ReplayBatchSize, cancellationToken);
            if (pending.Count == 0)
            {
                return;
            }

            foreach (PrivacyErasureIntent intent in pending)
            {
                PrivacyErasureApplier.PreparedErasure prepared = await applier.PrepareAsync(intent, cancellationToken);
                PrivacyErasureApplier.AppliedErasure applied =
                    await unitOfWork.ExecuteSerializableAsync(
                        ct => applier.ApplyInCurrentTransactionAsync(intent, prepared, ct),
                        cancellationToken);
                await applier.InvalidateAfterCommitAsync(applied);
                afterSequence = intent.AuthoritySequence;
            }
        }
    }

    private async Task<PrivacyErasureIntent> AppendWithAmbiguousAcknowledgementRetryAsync(
        PrivacyErasureRequest intent,
        CancellationToken cancellationToken)
    {
        try
        {
            return await authority.AppendAsync(intent, cancellationToken);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested
            && exception is TimeoutException or IOException or InvalidOperationException or OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await authority.AppendAsync(intent, cancellationToken);
        }
    }
}
