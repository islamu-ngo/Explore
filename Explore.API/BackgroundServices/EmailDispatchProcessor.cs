// ABOUTME: Basic Dispatch Mode worker that turns durable EmailDispatchOutbox rows into SMTP sends.
// ABOUTME: Claims PostgreSQL state first, sets tenant context for SMTP resolution, then records attempts/receipts/final state.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class EmailDispatchProcessor(
    IServiceProvider serviceProvider,
    IOptions<EmailDispatchProcessorSettings> settings,
    BusinessMetrics metrics,
    ILogger<EmailDispatchProcessor> logger) : BackgroundService
{
    private readonly EmailDispatchProcessorSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Email dispatch processor is disabled");
            return;
        }

        logger.LogInformation(
            "Email dispatch processor starting with {Interval}s interval and batch size {BatchSize}",
            _settings.PollingIntervalSeconds,
            _settings.BatchSize);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in email dispatch processor loop");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Email dispatch processor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var pending = await repository.GetPendingBatch(_settings.BatchSize, DateTime.UtcNow, stoppingToken);

        if (pending.Count == 0)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("No pending email dispatch rows");
            }

            return;
        }

        logger.LogInformation("Processing {Count} email dispatch rows", pending.Count);

        foreach (var dispatch in pending)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessSingleAsync(dispatch, stoppingToken);
        }
    }

    private async Task ProcessSingleAsync(EmailDispatchOutbox dispatch, CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

        if (await repository.IsTenantPaused(dispatch.TenantId, stoppingToken))
        {
            metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "tenant_paused", "tenant_paused");
            logger.LogInformation(
                "Email dispatch for tenant {TenantId} is paused; row {Id} remains pending",
                dispatch.TenantId,
                dispatch.Id);
            return;
        }

        var now = DateTime.UtcNow;
        var leaseToken = Guid.CreateVersion7();
        var claimed = await repository.TryMarkAsProcessing(dispatch.Id, leaseToken, now, stoppingToken);
        if (!claimed)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("Email dispatch row {Id} was already claimed", dispatch.Id);
            }

            return;
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
            var receiptClaimed = await repository.TryClaimReceipt(receipt, stoppingToken);
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
            var result = await emailService.SendAsync(message, stoppingToken);
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
            await repository.RecordAttempt(attempt, stoppingToken);

            if (result.Success)
            {
                await repository.MarkAsSent(dispatch.Id, completedAt, result.Message, stoppingToken);
                if (receipt is not null)
                {
                    await repository.MarkReceiptCompleted(receipt.Id, completedAt, result.Message, stoppingToken);
                }

                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "sent");
                logger.LogInformation(
                    "Email dispatch {Id} sent for tenant {TenantId} and source {SourceType}/{SourceId}",
                    dispatch.Id,
                    dispatch.TenantId,
                    dispatch.SourceType,
                    dispatch.SourceId);
                return;
            }

            var error = result.ErrorMessage ?? "Email send failed without provider details.";
            if (IsUnknownOutcome(error))
            {
                await repository.MarkAsUnknown(dispatch.Id, "smtp_outcome_unknown", error, completedAt, stoppingToken);
                if (receipt is not null)
                {
                    await repository.MarkReceiptFailed(receipt.Id, "smtp_outcome_unknown", error, completedAt, stoppingToken);
                }

                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "unknown", "smtp_outcome_unknown");
                logger.LogWarning(
                    "Email dispatch {Id} outcome is unknown with failure category {FailureCategory}",
                    dispatch.Id,
                    "smtp_outcome_unknown");
                return;
            }

            var failureCategory = ClassifyFailureCategory(error);
            var failedAttemptCount = dispatch.AttemptCount + 1;
            var failureOutcome = IsRetryExhausted(dispatch, failedAttemptCount) ? "dead_lettered" : "retry_scheduled";
            var delay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(dispatch.AttemptCount + 1));
            await repository.MarkAsFailed(
                dispatch.Id,
                failureCategory,
                error,
                isRetryable: true,
                delay,
                _settings.MaxAttemptCount,
                completedAt,
                stoppingToken);
            if (receipt is not null)
            {
                await repository.MarkReceiptFailed(receipt.Id, "smtp_retry_scheduled", error, completedAt, stoppingToken);
            }

            metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), failureOutcome, failureCategory);
            logger.LogWarning(
                "Email dispatch {Id} failed on attempt {Attempt}; outcome {Outcome}; retry delay {Delay}s; failure category {FailureCategory}",
                dispatch.Id,
                dispatch.AttemptCount + 1,
                failureOutcome,
                delay.TotalSeconds,
                failureCategory);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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
            }, stoppingToken);

            if (IsUnknownOutcome(error))
            {
                await repository.MarkAsUnknown(dispatch.Id, "smtp_outcome_unknown", error, failedAt, stoppingToken);
                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "unknown", "smtp_outcome_unknown");
            }
            else
            {
                var failureCategory = ClassifyFailureCategory(error);
                var failedAttemptCount = dispatch.AttemptCount + 1;
                var failureOutcome = IsRetryExhausted(dispatch, failedAttemptCount) ? "dead_lettered" : "retry_scheduled";
                var delay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(dispatch.AttemptCount + 1));
                await repository.MarkAsFailed(
                    dispatch.Id,
                    failureCategory,
                    error,
                    isRetryable: true,
                    delay,
                    _settings.MaxAttemptCount,
                    failedAt,
                    stoppingToken);
                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), failureOutcome, failureCategory);
            }

            if (receipt is not null)
            {
                await repository.MarkReceiptFailed(receipt.Id, ClassifyFailureCategory(error), error, failedAt, stoppingToken);
            }

            logger.LogError(ex, "Email dispatch {Id} failed with exception", dispatch.Id);
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
}
