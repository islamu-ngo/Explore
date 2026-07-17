// ABOUTME: Drains durable EmailDispatchOutbox rows into SMTP attempts for Basic Dispatch Mode.
// ABOUTME: Preserves PostgreSQL-owned delivery state while exposing a scheduler-friendly execution boundary.

using System.Net;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Models;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.Extensions.Configuration;
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
    private const string ProcessingLeaseExpiredFailureCategory = "processing_lease_expired";
    private const string ProcessingLeaseExpiredMessage = "Email dispatch processing lease expired before a durable outcome was recorded. Outcome is unknown and requires operator review or replay.";
    private const string AcceptedSettlementUnknownFailureCategory = "accepted_settlement_unknown";
    private const string SmtpOutcomeUnknownMessage = "SMTP provider acceptance is uncertain. Automatic resend is disabled pending reconciliation.";
    private const string SmtpSendFailedMessage = "SMTP send failed before provider acceptance was confirmed.";

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

            return new EmailDispatchDrainResult(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        logger.LogInformation("Processing {Count} email dispatch rows", pending.Count);

        var sent = 0;
        var retryScheduled = 0;
        var deadLettered = 0;
        var unknown = 0;
        var skipped = 0;
        var tenantPaused = 0;
        var alreadyClaimed = 0;
        var processed = 0;

        foreach (var dispatch in pending)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var result = await ProcessDispatchAsync(dispatch, _settings.ConsumerId, cancellationToken);
            var outcome = result.Outcome;
            switch (outcome)
            {
                case EmailDispatchDrainOutcome.Sent:
                    sent++;
                    processed++;
                    break;
                case EmailDispatchDrainOutcome.RetryScheduled:
                    retryScheduled++;
                    processed++;
                    break;
                case EmailDispatchDrainOutcome.DeadLettered:
                    deadLettered++;
                    processed++;
                    break;
                case EmailDispatchDrainOutcome.Unknown:
                    unknown++;
                    processed++;
                    break;
                case EmailDispatchDrainOutcome.Skipped:
                    skipped++;
                    processed++;
                    break;
                case EmailDispatchDrainOutcome.TenantPaused:
                    tenantPaused++;
                    break;
                case EmailDispatchDrainOutcome.AlreadyClaimed:
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
            skipped,
            tenantPaused,
            alreadyClaimed);
    }

    public async Task<EmailDispatchRecoveryResult> RecoverStaleProcessingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var recoveredAt = DateTime.UtcNow;
        var processingStartedBefore = recoveredAt.AddSeconds(-_settings.ProcessingLeaseTimeoutSeconds);
        var recovered = await repository.MarkStaleProcessingAsUnknown(
            processingStartedBefore,
            recoveredAt,
            ProcessingLeaseExpiredFailureCategory,
            ProcessingLeaseExpiredMessage,
            _settings.BatchSize,
            cancellationToken);

        if (recovered > 0)
        {
            logger.LogWarning(
                "Recovered {RecoveredCount} stale email dispatch processing rows as unknown. ProcessingStartedBefore={ProcessingStartedBefore:o}",
                recovered,
                processingStartedBefore);
        }
        else if (_settings.VerboseLogging)
        {
            logger.LogDebug(
                "No stale email dispatch processing rows found before {ProcessingStartedBefore:o}",
                processingStartedBefore);
        }

        return new EmailDispatchRecoveryResult(recovered, processingStartedBefore);
    }

    public async Task<EmailDispatchSingleDrainResult> ProcessSingleAsync(
        Guid tenantId,
        Guid publishEventId,
        string consumerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consumerId))
        {
            throw new ArgumentException("Consumer id is required for email dispatch drainage.", nameof(consumerId));
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var dispatch = await repository.GetByTenantAndPublishEventId(tenantId, publishEventId, cancellationToken);
        if (dispatch is null)
        {
            metrics.RecordEmailDispatchRabbitMqConsume(tenantId.ToString(), "rejected", "missing_outbox");
            logger.LogWarning(
                "Email dispatch pointer for tenant {TenantId} and publish event {PublishEventId} has no durable outbox row",
                tenantId,
                publishEventId);
            return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Missing);
        }

        if (IsSettled(dispatch.Status))
        {
            metrics.RecordEmailDispatchRabbitMqConsume(tenantId.ToString(), "acked", "already_settled");
            logger.LogInformation(
                "Email dispatch pointer {PublishEventId} for outbox row {OutboxId} is already settled with status {Status}",
                publishEventId,
                dispatch.Id,
                dispatch.Status);
            return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.AlreadySettled, dispatch.Id);
        }

        if (dispatch.Status == EmailDispatchStatus.Processing)
        {
            metrics.RecordEmailDispatchRabbitMqConsume(tenantId.ToString(), "acked", "already_processing");
            return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.AlreadyClaimed, dispatch.Id);
        }

        if (dispatch.Status == EmailDispatchStatus.RetryScheduled
            && dispatch.NextAttemptAt is { } nextAttemptAt
            && nextAttemptAt > DateTime.UtcNow)
        {
            metrics.RecordEmailDispatchRabbitMqConsume(tenantId.ToString(), "acked", "retry_deferred");
            return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Deferred, dispatch.Id);
        }

        return await ProcessDispatchAsync(dispatch, consumerId, cancellationToken);
    }

    private async Task<EmailDispatchSingleDrainResult> ProcessDispatchAsync(
        EmailDispatchOutbox dispatch,
        string consumerId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var eligibilityEvaluator = scope.ServiceProvider.GetRequiredService<IEmailDispatchEligibilityEvaluator>();
        var unsubscribeTokenService = scope.ServiceProvider.GetRequiredService<IEmailUnsubscribeTokenService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();

        if (await repository.IsTenantPaused(dispatch.TenantId, cancellationToken))
        {
            metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "tenant_paused", "tenant_paused");
            logger.LogInformation(
                "Email dispatch for tenant {TenantId} is paused; row {Id} remains pending",
                dispatch.TenantId,
                dispatch.Id);
            return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.TenantPaused, dispatch.Id);
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

            return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.AlreadyClaimed, dispatch.Id);
        }

        Guid? receiptId = null;
        var providerHandoffStarted = false;
        var attemptNumber = dispatch.AttemptCount + 1;
        tenantAccessor.SetTenant(dispatch.TenantId);

        try
        {
            var eligibility = await eligibilityEvaluator.EvaluateAndBeginProviderHandoffAsync(
                new EmailDispatchEligibilityRequest(
                    dispatch.TenantId,
                    dispatch.Id,
                    leaseToken,
                    attemptNumber,
                    consumerId,
                    now),
                cancellationToken);
            switch (eligibility.Outcome)
            {
                case EmailDispatchEligibilityOutcome.Skipped:
                    metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "skipped", eligibility.SkipReason);
                    logger.LogInformation(
                        "Email dispatch {Id} skipped before provider handoff with reason {SkipReason}",
                        dispatch.Id,
                        eligibility.SkipReason);
                    return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Skipped, dispatch.Id);
                case EmailDispatchEligibilityOutcome.TenantPaused:
                    return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.TenantPaused, dispatch.Id);
                case EmailDispatchEligibilityOutcome.LostClaim:
                    return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.AlreadyClaimed, dispatch.Id);
            }

            dispatch.RecipientEmail = eligibility.RecipientEmail
                ?? throw new InvalidOperationException("Eligible email dispatch is missing its current authorized destination.");
            receiptId = eligibility.ReceiptId;
            providerHandoffStarted = true;
            var preferenceCategory = ResolvePreferenceCategory(dispatch.Kind);
            var message = BuildEmailMessage(dispatch, unsubscribeTokenService, configuration, preferenceCategory, now);

            var startedAt = DateTime.UtcNow;
            var result = await emailService.SendAsync(message, cancellationToken);
            var completedAt = DateTime.UtcNow;

            if (result.Success)
            {
                var settlement = new EmailDispatchAcceptedSettlement(
                    dispatch.TenantId,
                    dispatch.Id,
                    attemptNumber,
                    completedAt,
                    ProviderMessageId: null);

                try
                {
                    await repository.SettleProviderAccepted(settlement, cancellationToken);
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    return await ReconcileProviderAcceptedAsync(settlement, dispatch);
                }
                catch (OperationCanceledException)
                {
                    return await ReconcileProviderAcceptedAsync(settlement, dispatch);
                }

                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "sent");
                logger.LogInformation(
                    "Email dispatch {Id} sent for tenant {TenantId} and source {SourceType}/{SourceId}",
                    dispatch.Id,
                    dispatch.TenantId,
                    dispatch.SourceType,
                    dispatch.SourceId);
                return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Sent, dispatch.Id);
            }

            var providerError = result.ErrorMessage;
            var outcomeUnknown = IsUnknownOutcome(providerError);
            var failureCategory = outcomeUnknown ? "smtp_outcome_unknown" : "smtp_send_failed";
            var safeFailureMessage = outcomeUnknown ? SmtpOutcomeUnknownMessage : SmtpSendFailedMessage;
            await repository.RecordAttempt(new EmailDispatchAttempt
            {
                TenantId = dispatch.TenantId,
                EmailDispatchOutboxId = dispatch.Id,
                AttemptNumber = attemptNumber,
                Outcome = outcomeUnknown ? EmailDispatchAttemptOutcome.Unknown : EmailDispatchAttemptOutcome.Failed,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                FailureCategory = failureCategory,
                SanitizedErrorMessage = safeFailureMessage,
                CorrelationId = dispatch.CorrelationId
            }, cancellationToken);

            if (outcomeUnknown)
            {
                return await ReconcileProviderAcceptedAsync(
                    new EmailDispatchAcceptedSettlement(
                        dispatch.TenantId,
                        dispatch.Id,
                        attemptNumber,
                        completedAt,
                        null),
                    dispatch);
            }

            var failedAttemptCount = dispatch.AttemptCount + 1;
            var isRetryExhausted = IsRetryExhausted(dispatch, failedAttemptCount);
            var failureOutcome = isRetryExhausted ? "dead_lettered" : "retry_scheduled";
            var delay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(dispatch.AttemptCount + 1));
            await repository.MarkAsFailed(
                dispatch.Id,
                failureCategory,
                safeFailureMessage,
                isRetryable: true,
                delay,
                _settings.MaxAttemptCount,
                completedAt,
                cancellationToken);
            if (receiptId is Guid failedReceiptId)
            {
                await repository.MarkReceiptFailed(failedReceiptId, "smtp_retry_scheduled", safeFailureMessage, completedAt, cancellationToken);
            }

            metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), failureOutcome, failureCategory);
            logger.LogWarning(
                "Email dispatch {Id} failed on attempt {Attempt}; outcome {Outcome}; retry delay {Delay}s; failure category {FailureCategory}",
                dispatch.Id,
                dispatch.AttemptCount + 1,
                failureOutcome,
                delay.TotalSeconds,
                failureCategory);

            return new EmailDispatchSingleDrainResult(
                isRetryExhausted ? EmailDispatchDrainOutcome.DeadLettered : EmailDispatchDrainOutcome.RetryScheduled,
                dispatch.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && !providerHandoffStarted)
        {
            throw;
        }
        catch (Exception) when (providerHandoffStarted)
        {
            return await ReconcileProviderAcceptedAsync(
                new EmailDispatchAcceptedSettlement(
                    dispatch.TenantId,
                    dispatch.Id,
                    attemptNumber,
                    DateTime.UtcNow,
                    null),
                dispatch);
        }
        catch
        {
            throw;
        }
        finally
        {
            tenantAccessor.Clear();
        }
    }

    private async Task<EmailDispatchSingleDrainResult> ReconcileProviderAcceptedAsync(
        EmailDispatchAcceptedSettlement settlement,
        EmailDispatchOutbox dispatch)
    {
        try
        {
            await using var reconciliationScope = scopeFactory.CreateAsyncScope();
            var reconciliationRepository = reconciliationScope.ServiceProvider.GetRequiredService<IEmailDispatchOutboxRepository>();
            var reconciliation = await reconciliationRepository.ReconcileProviderAccepted(
                settlement,
                CancellationToken.None);
            if (reconciliation == EmailDispatchAcceptedReconciliationOutcome.Sent)
            {
                metrics.RecordEmailDispatchAttempt(dispatch.TenantId.ToString(), "sent");
                logger.LogInformation(
                    "Email dispatch {Id} was already durably settled after an uncertain local commit",
                    dispatch.Id);
                return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Sent, dispatch.Id);
            }
        }
        catch (Exception)
        {
            logger.LogCritical(
                "Email dispatch {Id} accepted-settlement reconciliation failed; the durable provider-handoff fence prevents automatic resend",
                dispatch.Id);
        }

        metrics.RecordEmailDispatchAttempt(
            dispatch.TenantId.ToString(),
            "unknown",
            AcceptedSettlementUnknownFailureCategory);
        logger.LogWarning(
            "Email dispatch {Id} accepted-settlement outcome is unknown; automatic resend is disabled",
            dispatch.Id);
        return new EmailDispatchSingleDrainResult(EmailDispatchDrainOutcome.Unknown, dispatch.Id);
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

    private static EmailMessage BuildEmailMessage(
        EmailDispatchOutbox dispatch,
        IEmailUnsubscribeTokenService unsubscribeTokenService,
        IConfiguration configuration,
        string? preferenceCategory,
        DateTime issuedAt)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Correlation-ID"] = dispatch.CorrelationId ?? dispatch.Id.ToString(),
            ["X-Email-Dispatch-ID"] = dispatch.Id.ToString()
        };

        var plainTextBody = dispatch.PlainTextBody;
        var htmlBody = dispatch.HtmlBody;
        var unsubscribeUrl = BuildUnsubscribeUrl(dispatch, preferenceCategory, unsubscribeTokenService, configuration, issuedAt);
        if (unsubscribeUrl is not null)
        {
            headers["List-Unsubscribe"] = $"<{unsubscribeUrl}>";
            headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";
            plainTextBody = AppendPlainTextUnsubscribe(plainTextBody, unsubscribeUrl);
            htmlBody = AppendHtmlUnsubscribe(htmlBody, unsubscribeUrl);
        }

        return new EmailMessage
        {
            To = dispatch.RecipientEmail,
            Subject = dispatch.Subject,
            PlainTextBody = plainTextBody,
            HtmlBody = htmlBody,
            ReplyTo = dispatch.ReplyTo,
            CustomHeaders = headers
        };
    }

    private static string? BuildUnsubscribeUrl(
        EmailDispatchOutbox dispatch,
        string? preferenceCategory,
        IEmailUnsubscribeTokenService unsubscribeTokenService,
        IConfiguration configuration,
        DateTime issuedAt)
    {
        if (preferenceCategory is null)
        {
            return null;
        }

        var publicBaseUrl = ResolvePublicBaseUrl(configuration);
        if (publicBaseUrl is null)
        {
            return null;
        }

        var token = unsubscribeTokenService.GenerateToken(new EmailUnsubscribeTokenPayload(
            dispatch.TenantId,
            dispatch.RecipientUserId,
            preferenceCategory,
            issuedAt));
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return $"{publicBaseUrl}/api/email/unsubscribe?token={Uri.EscapeDataString(token)}";
    }

    private static string? ResolvePublicBaseUrl(IConfiguration configuration)
    {
        var configured = configuration["PublicBaseUrl"]
            ?? configuration["App:PublicBaseUrl"]
            ?? configuration["Application:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(configured)
            || !Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        return uri.ToString().TrimEnd('/');
    }

    private static string AppendPlainTextUnsubscribe(string? body, string unsubscribeUrl)
    {
        var prefix = string.IsNullOrWhiteSpace(body) ? string.Empty : body.TrimEnd() + "\n\n";
        return $"{prefix}To stop receiving this category of email, unsubscribe: {unsubscribeUrl}";
    }

    private static string AppendHtmlUnsubscribe(string? body, string unsubscribeUrl)
    {
        var encodedUrl = WebUtility.HtmlEncode(unsubscribeUrl);
        var footer = $"<p>To stop receiving this category of email, <a href=\"{encodedUrl}\">unsubscribe</a>.</p>";
        return string.IsNullOrWhiteSpace(body) ? footer : body + footer;
    }

    private static string? ResolvePreferenceCategory(EmailDispatchKind kind) => kind switch
    {
        EmailDispatchKind.RegistrationConfirmation => NotificationPreferenceCategories.RegistrationConfirmations,
        EmailDispatchKind.EventReminder => NotificationPreferenceCategories.EventReminders,
        EmailDispatchKind.OrganizerNotification => NotificationPreferenceCategories.OrganizerAnnouncements,
        EmailDispatchKind.RegistrationApproved
            or EmailDispatchKind.RegistrationRejected
            or EmailDispatchKind.WaitlistPromoted
            or EmailDispatchKind.EventCancelled => NotificationPreferenceCategories.EventUpdates,
        _ => null
    };

    private static bool IsSettled(EmailDispatchStatus status)
    {
        return status is EmailDispatchStatus.Sent
            or EmailDispatchStatus.DeadLettered
            or EmailDispatchStatus.Parked
            or EmailDispatchStatus.Unknown
            or EmailDispatchStatus.Skipped;
    }
}
