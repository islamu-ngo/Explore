// ABOUTME: Executes retained Coop decisions and atomically settles their durable effect pointers.
// ABOUTME: Revalidates pointer identity before command dispatch and stores only bounded safe failure metadata.

using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventReporting;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Features.EventReporting.Validators;
using Explore.Application.Serialization;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Options;

namespace Explore.Application.Services.Webhooks;

public sealed class IncomingWebhookEffectProcessingService(
    IIncomingWebhookEffectOutboxRepository pointerRepository,
    IIncomingWebhookMessageRepository messageRepository,
    IIncomingWebhookEffectReceiptRepository receiptRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator,
    IOptions<IncomingWebhookProcessingSettings> settings,
    TimeProvider timeProvider) : IIncomingWebhookEffectProcessingService
{
    private const string InvalidPayloadCategory = "coop_effect_payload_invalid";
    private const string TenantMismatchCategory = "coop_effect_tenant_mismatch";
    private const string IdentityMismatchCategory = "coop_effect_identity_mismatch";
    private const string PayloadUnavailableCategory = "coop_effect_payload_unavailable";
    private const string CommandRejectedCategory = "coop_effect_command_rejected";
    private const string TransientFailureCategory = "coop_effect_transient_failure";

    private readonly IncomingWebhookProcessingSettings _settings = settings.Value;

    public async Task<IncomingWebhookClaimExecutionResult> ProcessAsync(
        IncomingWebhookEffectClaim claim,
        CancellationToken cancellationToken)
    {
        var observedAt = GetUtcNow();
        var active = await pointerRepository.GetActiveClaimAsync(claim, observedAt, cancellationToken);
        if (active is null)
        {
            return IncomingWebhookClaimExecutionResult.LeaseLost();
        }

        var message = active.IncomingWebhookMessage ??
            await messageRepository.GetByTenantAndIdForUpdateAsync(
                active.TenantId,
                active.IncomingWebhookMessageId,
                cancellationToken);
        if (message is null || message.PayloadBytes.IsEmpty)
        {
            return await TransitionAsync(
                claim,
                PayloadUnavailableCategory,
                "The retained Coop callback payload is unavailable.",
                retry: false,
                cancellationToken);
        }

        if (message.TenantId != active.TenantId)
        {
            return await TransitionAsync(
                claim,
                TenantMismatchCategory,
                "The retained Coop callback tenant does not match its effect pointer.",
                retry: false,
                cancellationToken);
        }

        if (!HasMatchingEnvelope(active, message))
        {
            return await TransitionAsync(
                claim,
                IdentityMismatchCategory,
                "The retained Coop callback identity does not match its effect pointer.",
                retry: false,
                cancellationToken);
        }

        CoopDecisionCallbackRequestDto? request;
        try
        {
            request = JsonSerializer.Deserialize(
                message.PayloadBytes.Span,
                ExploreJsonContext.Default.CoopDecisionCallbackRequestDto);
        }
        catch (JsonException)
        {
            return await TransitionAsync(
                claim,
                InvalidPayloadCategory,
                "The retained Coop callback payload is not valid JSON.",
                retry: false,
                cancellationToken);
        }

        if (request is null)
        {
            return await TransitionAsync(
                claim,
                InvalidPayloadCategory,
                "The retained Coop callback payload is empty.",
                retry: false,
                cancellationToken);
        }

        if (ProcessCoopDecisionCallbackCommandValidator.ResolveTenantId(request) != active.TenantId)
        {
            return await TransitionAsync(
                claim,
                TenantMismatchCategory,
                "The Coop callback payload tenant does not match its effect pointer.",
                retry: false,
                cancellationToken);
        }

        var providerDecisionId = ProcessCoopDecisionCallbackCommandValidator.FirstNonBlank(
            request.ProviderDecisionId,
            request.ProviderDecisionIdSnake);
        if (!string.Equals(providerDecisionId, active.ProviderDecisionId, StringComparison.Ordinal))
        {
            return await TransitionAsync(
                claim,
                IdentityMismatchCategory,
                "The Coop callback decision identity does not match its effect pointer.",
                retry: false,
                cancellationToken);
        }

        try
        {
            var response = await mediator.Send(
                new ProcessCoopDecisionCallbackCommand { Request = request },
                cancellationToken);
            if (response.Success)
            {
                return await CompleteAsync(claim, cancellationToken);
            }

            var retry = string.Equals(
                response.FailureCode,
                EventReportFailureCodes.CaseConcurrencyConflict,
                StringComparison.Ordinal);
            return await TransitionAsync(
                claim,
                retry ? TransientFailureCategory : CommandRejectedCategory,
                retry
                    ? "The Coop decision conflicted with a concurrent report-case transition."
                    : "The Coop decision was rejected by the local moderation workflow.",
                retry,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IncomingWebhookEffectReceiptConflictException)
        {
            return await RecoverConcurrentReceiptAsync(claim, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return await TransitionAsync(
                claim,
                TransientFailureCategory,
                "The Coop decision could not be completed because of a transient failure.",
                retry: true,
                cancellationToken);
        }
    }

    private Task<IncomingWebhookClaimExecutionResult> CompleteAsync(
        IncomingWebhookEffectClaim claim,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pointer = await pointerRepository.GetByTenantAndIdForUpdateAsync(
                claim.TenantId,
                claim.EffectOutboxId,
                token);
            if (pointer is null)
            {
                return IncomingWebhookClaimExecutionResult.LeaseLost();
            }

            var receipt = await receiptRepository.GetByIdentityAsync(
                pointer.TenantId,
                pointer.IncomingWebhookMessageId,
                pointer.EffectKind,
                token);
            if (receipt is null)
            {
                receipt = IncomingWebhookEffectReceipt.Create(
                    pointer.TenantId,
                    pointer.IncomingWebhookMessageId,
                    pointer.EffectKind,
                    pointer.PayloadSha256,
                    pointer.ProcessingGeneration,
                    GetUtcNow(),
                    $"coop-effect:{pointer.Id:N}");
                await receiptRepository.AddAsync(receipt, token);
            }
            else
            {
                receipt.EnsureMatches(
                    pointer.TenantId,
                    pointer.IncomingWebhookMessageId,
                    pointer.EffectKind,
                    pointer.PayloadSha256,
                    pointer.ProcessingGeneration);
            }

            try
            {
                pointer.Complete(
                    claim.LeaseToken,
                    claim.ProcessingFence,
                    claim.ProcessingGeneration,
                    GetUtcNow());
            }
            catch (InvalidOperationException)
            {
                return IncomingWebhookClaimExecutionResult.LeaseLost();
            }

            await pointerRepository.SaveChangesAsync(token);
            return IncomingWebhookClaimExecutionResult.Completed("succeeded");
        }, cancellationToken);

    private Task<IncomingWebhookClaimExecutionResult> TransitionAsync(
        IncomingWebhookEffectClaim claim,
        string failureCategory,
        string safeDetail,
        bool retry,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pointer = await pointerRepository.GetByTenantAndIdForUpdateAsync(
                claim.TenantId,
                claim.EffectOutboxId,
                token);
            if (pointer is null)
            {
                return IncomingWebhookClaimExecutionResult.LeaseLost();
            }

            var transitionAt = GetUtcNow();
            try
            {
                if (retry && pointer.AttemptCount < _settings.MaxAttempts)
                {
                    pointer.ScheduleRetry(
                        claim.LeaseToken,
                        claim.ProcessingFence,
                        claim.ProcessingGeneration,
                        failureCategory,
                        safeDetail,
                        transitionAt.Add(ComputeRetryDelay(pointer.AttemptCount)),
                        transitionAt);
                }
                else
                {
                    pointer.DeadLetter(
                        claim.LeaseToken,
                        claim.ProcessingFence,
                        claim.ProcessingGeneration,
                        failureCategory,
                        safeDetail,
                        transitionAt);
                }
            }
            catch (InvalidOperationException)
            {
                return IncomingWebhookClaimExecutionResult.LeaseLost();
            }

            await pointerRepository.SaveChangesAsync(token);
            return IncomingWebhookClaimExecutionResult.Completed(
                retry && pointer.Status == OutboxMessageStatus.Failed
                    ? "retry_scheduled"
                    : "dead_lettered");
        }, cancellationToken);

    private async Task<IncomingWebhookClaimExecutionResult> RecoverConcurrentReceiptAsync(
        IncomingWebhookEffectClaim claim,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var pointer = await pointerRepository.GetByTenantAndIdForUpdateAsync(
                claim.TenantId,
                claim.EffectOutboxId,
                token);
            if (pointer is null)
            {
                return IncomingWebhookClaimExecutionResult.LeaseLost();
            }

            var receipt = await receiptRepository.GetByIdentityAsync(
                pointer.TenantId,
                pointer.IncomingWebhookMessageId,
                pointer.EffectKind,
                token);
            if (receipt is null)
            {
                throw new InvalidOperationException("The conflicting Coop effect receipt could not be recovered.");
            }

            receipt.EnsureMatches(
                pointer.TenantId,
                pointer.IncomingWebhookMessageId,
                pointer.EffectKind,
                pointer.PayloadSha256,
                pointer.ProcessingGeneration);
            try
            {
                pointer.Complete(
                    claim.LeaseToken,
                    claim.ProcessingFence,
                    claim.ProcessingGeneration,
                    GetUtcNow());
            }
            catch (InvalidOperationException)
            {
                return IncomingWebhookClaimExecutionResult.LeaseLost();
            }

            await pointerRepository.SaveChangesAsync(token);
            return IncomingWebhookClaimExecutionResult.Completed("recovered");
        }, cancellationToken);
    }

    private static bool HasMatchingEnvelope(
        IncomingWebhookEffectOutbox pointer,
        IncomingWebhookMessage message) =>
        string.Equals(pointer.Provider, "coop", StringComparison.Ordinal) &&
        string.Equals(message.Provider, pointer.Provider, StringComparison.Ordinal) &&
        string.Equals(message.EventType, pointer.EffectKind, StringComparison.Ordinal) &&
        string.Equals(message.ProviderMessageId, pointer.ProviderDecisionId, StringComparison.Ordinal) &&
        string.Equals(message.PayloadHash, pointer.PayloadSha256, StringComparison.Ordinal);

    private TimeSpan ComputeRetryDelay(int attemptCount)
    {
        var exponent = Math.Clamp(attemptCount - 1, 0, 30);
        var multiplier = 1L << exponent;
        var seconds = Math.Min(
            checked((long)_settings.InitialRetryDelaySeconds * multiplier),
            _settings.MaxRetryDelaySeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private DateTime GetUtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
