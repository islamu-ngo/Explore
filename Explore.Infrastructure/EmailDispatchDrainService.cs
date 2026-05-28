// ABOUTME: Drains durable EmailDispatchOutbox rows into SMTP attempts for Basic Dispatch Mode.
// ABOUTME: Preserves PostgreSQL-owned delivery state while exposing a scheduler-friendly execution boundary.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;
using Explore.Application.Telemetry;
using Explore.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure;

public sealed class EmailDispatchDrainService(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailDispatchProcessorSettings> settings,
    BusinessMetrics metrics,
    ILogger<EmailDispatchDrainService> logger) : IEmailDispatchDrainService
{
    private readonly EmailDispatchProcessorSettings _settings = settings.Value;

    public async Task<EmailDispatchDrainResult> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var pending = await repository.GetPendingBatch(_settings.BatchSize, DateTime.UtcNow, cancellationToken);

        if (pending.Count == 0)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("No pending email dispatch rows");
            }

            return new EmailDispatchDrainResult(0, 0, 0, 0, 0, 0, 0, 0);
        }

        logger.LogInformation("Processing {Count} email dispatch rows", pending.Count);

        var sent = 0;
        var retryScheduled = 0;
        var deadLettered = 0;
        var unknown = 0;
        var tenantPaused = 0;
        var alreadyClaimed = 0;
        var processed = 0;

        foreach (var dispatch in pending)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var outcome = await ProcessSingleAsync(dispatch, cancellationToken);
            switch (outcome)
            {
                case DrainOutcome.Sent:
                    sent++;
                    processed++;
                    break;
                case DrainOutcome.RetryScheduled:
                    retryScheduled++;
                    processed++;
                    break;
                case DrainOutcome.DeadLettered:
                    deadLettered++;
                    processed++;
                    break;
                case DrainOutcome.Unknown:
                    unknown++;
                    processed++;
                    break;
                case DrainOutcome.TenantPaused:
                    tenantPaused++;
                    break;
                case DrainOutcome.AlreadyClaimed:
                    alreadyClaimed++;
                    break;
            }
        }

        return new EmailDispatchDrainResult(
            pending.Count,
            processed,
            sent,
            retryScheduled,
            deadLettered,
            unknown,
            tenantPaused,
            alreadyClaimed);
    }

    private async Task<DrainOutcome> ProcessSingleAsync(EmailDispatchOutbox dispatch, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

        if (await repository.IsTenantPaused(dispatch.TenantId, cancellationToken))
        {
            metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "tenant_paused", "tenant_paused");
            logger.LogInformation(
                "Email dispatch for tenant {TenantId} is paused; row {Id} remains pending",
                dispatch.TenantId,
                dispatch.Id);
            return DrainOutcome.TenantPaused;
        }

        var now = DateTime.UtcNow;
        var leaseToken = Guid.CreateVersion7();
        var claimed = await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, now, cancellationToken);
        if (!claimed)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("Email dispatch row {Id} was already claimed", dispatch.Id);
            }

            return DrainOutcome.AlreadyClaimed;
        }

        EmailDispatchReceipt? receipt = null;
        tenantAccessor.SetTenant(dispatch.TenantId);

        try
        {
            receipt = new EmailDispatchReceipt
            {
                TenantId = dispatch.TenantId,
                PublishEventId = dispatch.PublishEventId,
                EmailDispatchOutboxId = dispatch.Id,
                Status = EmailDispatchReceiptStatus.Processing,
                ConsumerId = _settings.ConsumerId,
                FirstSeenAt = now,
                ProcessingStartedAt = now
            };
            var receiptClaimed = await repository.TryClaimReceipt(receipt, cancellationToken);
            if (!receiptClaimed)
            {
                receipt = null;
                logger.LogDebug("Email dispatch receipt already exists for publish event {PublishEventId}", dispatch.PublishEventId);
            }

            var message = new EmailMessage
            {
                To = dispatch.RecipientEmail,
                Subject = dispatch.Subject,
                PlainTextBody = dispatch.PlainTextBody,
                HtmlBody = dispatch.HtmlBody,
                ReplyTo = dispatch.ReplyTo,
                CustomHeaders = new Dictionary<string, string>
                {
                    ["X-Correlation-ID"] = dispatch.CorrelationId ?? dispatch.Id.ToString(),
                    ["X-Email-Dispatch-ID"] = dispatch.Id.ToString()
                }
            };

            var startedAt = DateTime.UtcNow;
            var result = await emailService.SendAsync(message, cancellationToken);
            var completedAt = DateTime.UtcNow;

            var attempt = new EmailDispatchAttempt
            {
                TenantId = dispatch.TenantId,
                EmailDispatchOutboxId = dispatch.Id,
                AttemptNumber = dispatch.AttemptCount + 1,
                Outcome = result.Success ? EmailDispatchAttemptOutcome.Succeeded : ClassifyOutcome(result.ErrorMessage),
                StartedAt = startedAt,
                CompletedAt = completedAt,
                FailureCategory = result.Success ? null : ClassifyFailureCategory(result.ErrorMessage),
                SanitizedErrorMessage = result.Success ? null : result.ErrorMessage,
                ProviderMessageId = result.Message,
                CorrelationId = dispatch.CorrelationId
            };
            await repository.RecordAttempt(attempt, cancellationToken);

            if (result.Success)
            {
                await repository.MarkAsSent(dispatch.Id, completedAt, result.Message, cancellationToken);
                if (receipt is not null)
                {
                    await repository.MarkReceiptCompleted(receipt.Id, completedAt, result.Message, cancellationToken);
                }

                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "sent");
                logger.LogInformation(
                    "Email dispatch {Id} sent for tenant {TenantId} and source {SourceType}/{SourceId}",
                    dispatch.Id,
                    dispatch.TenantId,
                    dispatch.SourceType,
                    dispatch.SourceId);
                return DrainOutcome.Sent;
            }

            var error = result.ErrorMessage ?? "Email send failed without provider details.";
            if (IsUnknownOutcome(error))
            {
                await repository.MarkAsUnknown(dispatch.Id, "smtp_outcome_unknown", error, completedAt, cancellationToken);
                if (receipt is not null)
                {
                    await repository.MarkReceiptFailed(receipt.Id, "smtp_outcome_unknown", error, completedAt, cancellationToken);
                }

                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "unknown", "smtp_outcome_unknown");
                logger.LogWarning(
                    "Email dispatch {Id} outcome is unknown with failure category {FailureCategory}",
                    dispatch.Id,
                    "smtp_outcome_unknown");
                return DrainOutcome.Unknown;
            }

            var failureCategory = ClassifyFailureCategory(error);
            var failedAttemptCount = dispatch.AttemptCount + 1;
            var isRetryExhausted = IsRetryExhausted(dispatch, failedAttemptCount);
            var failureOutcome = isRetryExhausted ? "dead_lettered" : "retry_scheduled";
            var delay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(dispatch.AttemptCount + 1));
            await repository.MarkAsFailed(
                dispatch.Id,
                failureCategory,
                error,
                isRetryable: true,
                delay,
                _settings.MaxAttemptCount,
                completedAt,
                cancellationToken);
            if (receipt is not null)
            {
                await repository.MarkReceiptFailed(receipt.Id, "smtp_retry_scheduled", error, completedAt, cancellationToken);
            }

            metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), failureOutcome, failureCategory);
            logger.LogWarning(
                "Email dispatch {Id} failed on attempt {Attempt}; outcome {Outcome}; retry delay {Delay}s; failure category {FailureCategory}",
                dispatch.Id,
                dispatch.AttemptCount + 1,
                failureOutcome,
                delay.TotalSeconds,
                failureCategory);

            return isRetryExhausted ? DrainOutcome.DeadLettered : DrainOutcome.RetryScheduled;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var failedAt = DateTime.UtcNow;
            var error = ex.Message;
            await repository.RecordAttempt(new EmailDispatchAttempt
            {
                TenantId = dispatch.TenantId,
                EmailDispatchOutboxId = dispatch.Id,
                AttemptNumber = dispatch.AttemptCount + 1,
                Outcome = IsUnknownOutcome(error) ? EmailDispatchAttemptOutcome.Unknown : EmailDispatchAttemptOutcome.Failed,
                StartedAt = now,
                CompletedAt = failedAt,
                FailureCategory = ClassifyFailureCategory(error),
                SanitizedErrorMessage = error,
                CorrelationId = dispatch.CorrelationId
            }, cancellationToken);

            DrainOutcome outcome;
            if (IsUnknownOutcome(error))
            {
                await repository.MarkAsUnknown(dispatch.Id, "smtp_outcome_unknown", error, failedAt, cancellationToken);
                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "unknown", "smtp_outcome_unknown");
                outcome = DrainOutcome.Unknown;
            }
            else
            {
                var failureCategory = ClassifyFailureCategory(error);
                var failedAttemptCount = dispatch.AttemptCount + 1;
                var isRetryExhausted = IsRetryExhausted(dispatch, failedAttemptCount);
                var failureOutcome = isRetryExhausted ? "dead_lettered" : "retry_scheduled";
                var delay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(dispatch.AttemptCount + 1));
                await repository.MarkAsFailed(
                    dispatch.Id,
                    failureCategory,
                    error,
                    isRetryable: true,
                    delay,
                    _settings.MaxAttemptCount,
                    failedAt,
                    cancellationToken);
                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), failureOutcome, failureCategory);
                outcome = isRetryExhausted ? DrainOutcome.DeadLettered : DrainOutcome.RetryScheduled;
            }

            if (receipt is not null)
            {
                await repository.MarkReceiptFailed(receipt.Id, ClassifyFailureCategory(error), error, failedAt, cancellationToken);
            }

            logger.LogError(ex, "Email dispatch {Id} failed with exception", dispatch.Id);
            return outcome;
        }
        finally
        {
            tenantAccessor.Clear();
        }
    }

    private static EmailDispatchAttemptOutcome ClassifyOutcome(string? error)
    {
        return IsUnknownOutcome(error) ? EmailDispatchAttemptOutcome.Unknown : EmailDispatchAttemptOutcome.Failed;
    }

    private static string ClassifyFailureCategory(string? error)
    {
        return IsUnknownOutcome(error) ? "smtp_outcome_unknown" : "smtp_send_failed";
    }

    private static bool IsUnknownOutcome(string? error)
    {
        return error?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true
            || error?.Contains("timed out", StringComparison.OrdinalIgnoreCase) == true
            || error?.Contains("operation canceled", StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool IsRetryExhausted(EmailDispatchOutbox dispatch, int failedAttemptCount)
    {
        return failedAttemptCount >= Math.Min(dispatch.MaxAttempts, _settings.MaxAttemptCount);
    }

    private enum DrainOutcome
    {
        Sent,
        RetryScheduled,
        DeadLettered,
        Unknown,
        TenantPaused,
        AlreadyClaimed
    }
}
