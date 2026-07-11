// ABOUTME: Drains LocalProvider webhook delivery attempts into signed outbound HTTP POST requests.
// ABOUTME: Applies SSRF checks, lease-based processing, retries, endpoint state, and canonical message status refresh.

using System.Diagnostics;
using System.Net;
using System.Text;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Webhooks;

public sealed class WebhookDeliveryDrainService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IWebhookSignatureService signatureService,
    WebhookEndpointSafetyPolicy safetyPolicy,
    WebhookRetryScheduler retryScheduler,
    WebhookEndpointSecretResolver secretResolver,
    IOptions<WebhookDeliveryProcessorSettings> settings,
    IOptionsMonitor<WebhookOptions> webhookOptions,
    BusinessMetrics metrics,
    ILogger<WebhookDeliveryDrainService> logger) : IWebhookDeliveryDrainService
{
    public const string HttpClientName = "LocalWebhookDeliveryClient";

    private const string ProcessingLeaseExpiredFailureCategory = "processing_lease_expired";
    private const string MissingEndpointFailureCategory = "missing_endpoint";
    private const string MissingMessageFailureCategory = "missing_message";
    private const string EndpointNotActiveFailureCategory = "endpoint_not_active";
    private const string PayloadUnavailableFailureCategory = "payload_unavailable";
    private const string PayloadTooLargeFailureCategory = "payload_too_large";
    private const string MissingSecretFailureCategory = "missing_secret";
    private const string InvalidSecretFailureCategory = "invalid_secret";
    private const string InvalidUrlFailureCategory = "invalid_url";
    private const string RedirectResponseFailureCategory = "redirect_response";
    private const string HttpNonSuccessFailureCategory = "http_non_success";
    private const string TimeoutFailureCategory = "timeout";
    private const string NetworkFailureCategory = "network_error";
    private const string ProviderDisabledFailureCategory = "provider_disabled";
    private const string AttemptStatusNotRetryableFailureCategory = "attempt_status_not_retryable";
    private const string MessagePayloadClearedFailureCategory = "message_payload_cleared";

    private readonly WebhookDeliveryProcessorSettings _settings = settings.Value;

    public async Task<WebhookDeliveryDrainResult> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        if (!ShouldProcessLocalAttempts())
        {
            return new WebhookDeliveryDrainResult(0, 0, 0, 0, 0, 0, 0);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var pending = await attemptRepository.GetDueScheduledAsync(
            _settings.BatchSize,
            DateTime.UtcNow,
            cancellationToken);

        if (pending.Count == 0)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("No due webhook delivery attempts");
            }

            return new WebhookDeliveryDrainResult(0, 0, 0, 0, 0, 0, 0);
        }

        var succeeded = 0;
        var retryScheduled = 0;
        var abandoned = 0;
        var skipped = 0;
        var alreadyClaimed = 0;
        var processed = 0;

        foreach (var attempt in pending)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var result = await ProcessAttemptAsync(attempt, cancellationToken);
            switch (result.Outcome)
            {
                case WebhookDeliveryDrainOutcome.Succeeded:
                    succeeded++;
                    processed++;
                    break;
                case WebhookDeliveryDrainOutcome.RetryScheduled:
                    retryScheduled++;
                    processed++;
                    break;
                case WebhookDeliveryDrainOutcome.Abandoned:
                    abandoned++;
                    processed++;
                    break;
                case WebhookDeliveryDrainOutcome.AlreadyClaimed:
                    alreadyClaimed++;
                    break;
                case WebhookDeliveryDrainOutcome.Skipped:
                case WebhookDeliveryDrainOutcome.AlreadySettled:
                case WebhookDeliveryDrainOutcome.Deferred:
                case WebhookDeliveryDrainOutcome.Missing:
                    skipped++;
                    break;
            }
        }

        return new WebhookDeliveryDrainResult(
            pending.Count,
            processed,
            succeeded,
            retryScheduled,
            abandoned,
            skipped,
            alreadyClaimed);
    }

    public async Task<WebhookDeliveryRecoveryResult> RecoverStaleProcessingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var recoveredAt = DateTime.UtcNow;
        var processingStartedBefore = recoveredAt.AddSeconds(-_settings.ProcessingLeaseTimeoutSeconds);
        var recovered = await attemptRepository.ResetStaleSendingAsync(
            processingStartedBefore,
            recoveredAt,
            ProcessingLeaseExpiredFailureCategory,
            _settings.BatchSize,
            cancellationToken);

        if (recovered > 0)
        {
            logger.LogWarning(
                "Recovered {RecoveredCount} stale webhook delivery attempts. ProcessingStartedBefore={ProcessingStartedBefore:o}",
                recovered,
                processingStartedBefore);
        }
        else if (_settings.VerboseLogging)
        {
            logger.LogDebug(
                "No stale webhook delivery attempts found before {ProcessingStartedBefore:o}",
                processingStartedBefore);
        }

        return new WebhookDeliveryRecoveryResult(recovered, processingStartedBefore);
    }

    public async Task<WebhookDeliverySingleDrainResult> ProcessSingleAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var attempt = await attemptRepository.GetByTenantAndIdAsync(tenantId, attemptId, cancellationToken);
        if (attempt is null)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Missing);
        }

        if (attempt.Status is WebhookDeliveryAttemptStatus.Succeeded or WebhookDeliveryAttemptStatus.Abandoned)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadySettled, attempt.Id);
        }

        if (attempt.Status == WebhookDeliveryAttemptStatus.Sending)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        if (attempt.Status == WebhookDeliveryAttemptStatus.Scheduled && attempt.ScheduledAt > DateTime.UtcNow)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Deferred, attempt.Id);
        }

        return ShouldProcessLocalAttempts()
            ? await ProcessAttemptAsync(attempt, cancellationToken)
            : new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Skipped, attempt.Id);
    }

    public async Task<WebhookDeliverySingleDrainResult> ScheduleManualRetryAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IWebhookMessageRepository>();
        var attempt = await attemptRepository.GetByTenantAndIdAsync(tenantId, attemptId, cancellationToken);
        if (attempt is null)
        {
            metrics.RecordWebhookManualRetry(
                tenantId.ToString("D"),
                eventType: null,
                outcome: "missing",
                failureCategory: MissingMessageFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Missing);
        }

        var eventType = attempt.Message?.EventType;
        if (!ShouldProcessLocalAttempts())
        {
            RecordManualRetry(attempt, "skipped", ProviderDisabledFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Skipped, attempt.Id);
        }

        if (attempt.Status == WebhookDeliveryAttemptStatus.Scheduled)
        {
            RecordManualRetry(attempt, "deferred", AttemptStatusNotRetryableFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Deferred, attempt.Id);
        }

        if (attempt.Status == WebhookDeliveryAttemptStatus.Sending)
        {
            RecordManualRetry(attempt, "already_claimed", AttemptStatusNotRetryableFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        if (attempt.Status == WebhookDeliveryAttemptStatus.Succeeded)
        {
            RecordManualRetry(attempt, "already_settled", AttemptStatusNotRetryableFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadySettled, attempt.Id);
        }

        if (attempt.Endpoint is null)
        {
            RecordManualRetry(attempt, "missing", MissingEndpointFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Missing, attempt.Id);
        }

        if (attempt.Message is null)
        {
            RecordManualRetry(attempt, "missing", MissingMessageFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Missing, attempt.Id);
        }

        if (attempt.Endpoint.Status != WebhookEndpointStatus.Active)
        {
            RecordManualRetry(attempt, "skipped", EndpointNotActiveFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Skipped, attempt.Id);
        }

        if (string.IsNullOrWhiteSpace(attempt.Message.PayloadJson))
        {
            var failureCategory = attempt.Message.PayloadClearedAt is null
                ? PayloadUnavailableFailureCategory
                : MessagePayloadClearedFailureCategory;
            RecordManualRetry(attempt, "skipped", failureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Skipped, attempt.Id);
        }

        var hasActiveAttempt = await attemptRepository.HasActiveAttemptForEndpointAsync(
            tenantId,
            attempt.MessageId,
            attempt.EndpointId,
            cancellationToken);
        if (hasActiveAttempt)
        {
            RecordManualRetry(attempt, "deferred", AttemptStatusNotRetryableFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Deferred, attempt.Id);
        }

        var now = DateTime.UtcNow;
        var retry = await attemptRepository.CreateAsync(new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = attempt.TenantId,
            MessageId = attempt.MessageId,
            EndpointId = attempt.EndpointId,
            AttemptNumber = await attemptRepository.GetNextAttemptNumberAsync(
                attempt.TenantId,
                attempt.MessageId,
                attempt.EndpointId,
                cancellationToken),
            Status = WebhookDeliveryAttemptStatus.Scheduled,
            ScheduledAt = now,
            CreatedAt = now
        }, cancellationToken);

        await messageRepository.RefreshLocalDeliveryStatusAsync(
            retry.TenantId,
            retry.MessageId,
            now,
            cancellationToken);

        metrics.RecordWebhookManualRetry(
            retry.TenantId.ToString("D"),
            eventType,
            "retry_scheduled");
        return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.RetryScheduled, retry.Id);
    }

    private async Task<WebhookDeliverySingleDrainResult> ProcessAttemptAsync(
        WebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var startedAt = DateTime.UtcNow;
        var leaseToken = Guid.CreateVersion7();
        var claimed = await attemptRepository.TryMarkAsSendingAsync(
            attempt.TenantId,
            attempt.Id,
            leaseToken,
            startedAt,
            cancellationToken);

        if (!claimed)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("Webhook delivery attempt {AttemptId} was already claimed", attempt.Id);
            }

            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        return await SendClaimedAttemptAsync(attempt, leaseToken, startedAt, cancellationToken);
    }

    private async Task<WebhookDeliverySingleDrainResult> SendClaimedAttemptAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        if (attempt.Endpoint is null)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                MissingEndpointFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        if (attempt.Message is null)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                MissingMessageFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        if (attempt.Endpoint.Status != WebhookEndpointStatus.Active)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                EndpointNotActiveFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        var payload = attempt.Message.PayloadJson;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                PayloadUnavailableFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        var localOptions = webhookOptions.CurrentValue.Local;
        if (Encoding.UTF8.GetByteCount(payload) > localOptions.MaxPayloadBytes)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                PayloadTooLargeFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        if (!Uri.TryCreate(attempt.Endpoint.Url, UriKind.Absolute, out var endpointUri))
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                InvalidUrlFailureCategory,
                disableEndpoint: true,
                cancellationToken);
        }

        var safetyResult = await safetyPolicy.ValidateAsync(endpointUri, cancellationToken);
        if (!safetyResult.IsAllowed)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                safetyResult.FailureCategory ?? InvalidUrlFailureCategory,
                disableEndpoint: true,
                cancellationToken);
        }

        var secret = secretResolver.Resolve(attempt.Endpoint);
        if (secret is null)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                startedAt,
                MissingSecretFailureCategory,
                disableEndpoint: true,
                cancellationToken);
        }

        var startedTimestamp = Stopwatch.GetTimestamp();
        try
        {
            using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var timeoutSeconds = attempt.Endpoint.TimeoutSeconds > 0
                ? attempt.Endpoint.TimeoutSeconds
                : localOptions.TimeoutSeconds;
            requestTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var request = BuildRequest(endpointUri, attempt.Message.Id, payload, secret);
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                requestTimeout.Token);

            var completedAt = DateTime.UtcNow;
            var durationMs = GetDurationMilliseconds(startedTimestamp);

            if (response.IsSuccessStatusCode)
            {
                return await MarkSucceededAsync(
                    attempt,
                    leaseToken,
                    startedAt,
                    completedAt,
                    (int)response.StatusCode,
                    durationMs,
                    responseBodyPreview: null,
                    cancellationToken);
            }

            var failureCategory = IsRedirect(response.StatusCode)
                ? RedirectResponseFailureCategory
                : HttpNonSuccessFailureCategory;
            var preview = await ReadResponsePreviewAsync(
                response.Content,
                localOptions.MaxResponsePreviewBytes,
                requestTimeout.Token);

            return await MarkFailureAsync(
                attempt,
                leaseToken,
                completedAt,
                failureCategory,
                (int)response.StatusCode,
                durationMs,
                preview,
                retryable: true,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return await MarkFailureAsync(
                attempt,
                leaseToken,
                DateTime.UtcNow,
                TimeoutFailureCategory,
                httpStatusCode: null,
                GetDurationMilliseconds(startedTimestamp),
                responseBodyPreview: null,
                retryable: true,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return await MarkFailureAsync(
                attempt,
                leaseToken,
                DateTime.UtcNow,
                NetworkFailureCategory,
                httpStatusCode: null,
                GetDurationMilliseconds(startedTimestamp),
                responseBodyPreview: null,
                retryable: true,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
        catch (FormatException)
        {
            return await MarkFailureAsync(
                attempt,
                leaseToken,
                DateTime.UtcNow,
                InvalidSecretFailureCategory,
                httpStatusCode: null,
                GetDurationMilliseconds(startedTimestamp),
                responseBodyPreview: null,
                retryable: false,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
    }

    private HttpRequestMessage BuildRequest(
        Uri endpointUri,
        Guid messageId,
        string payload,
        WebhookSecretMaterial secret)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var signatureHeaders = signatureService.Sign(messageId.ToString("N"), timestamp, payload, secret);

        var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("svix-id", signatureHeaders.SvixId);
        request.Headers.TryAddWithoutValidation("svix-timestamp", signatureHeaders.SvixTimestamp);
        request.Headers.TryAddWithoutValidation("svix-signature", signatureHeaders.SvixSignature);
        return request;
    }

    private async Task<WebhookDeliverySingleDrainResult> MarkSucceededAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        DateTime startedAt,
        DateTime completedAt,
        int httpStatusCode,
        int durationMs,
        string? responseBodyPreview,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var endpointRepository = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IWebhookMessageRepository>();

        await attemptRepository.MarkSucceededAsync(
            attempt.TenantId,
            attempt.Id,
            leaseToken,
            startedAt,
            completedAt,
            httpStatusCode,
            durationMs,
            responseBodyPreview,
            cancellationToken);

        await endpointRepository.MarkSuccessAsync(
            attempt.TenantId,
            attempt.EndpointId,
            completedAt,
            cancellationToken);

        await messageRepository.RefreshLocalDeliveryStatusAsync(
            attempt.TenantId,
            attempt.MessageId,
            completedAt,
            cancellationToken);

        metrics.RecordWebhookDeliveryAttempt(
            attempt.TenantId.ToString("D"),
            attempt.Message?.EventType,
            "succeeded");
        metrics.RecordWebhookDeliverySuccess(
            attempt.TenantId.ToString("D"),
            attempt.Message?.EventType);

        return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Succeeded, attempt.Id);
    }

    private async Task<WebhookDeliverySingleDrainResult> MarkFailureAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        DateTime completedAt,
        string failureCategory,
        int? httpStatusCode,
        int durationMs,
        string? responseBodyPreview,
        bool retryable,
        bool disableEndpointOnAbandon,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var endpointRepository = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var messageRepository = scope.ServiceProvider.GetRequiredService<IWebhookMessageRepository>();
        var maxAttempts = GetMaxAttempts(attempt.Endpoint);
        var nextAttemptNumber = attempt.AttemptNumber + 1;
        var canRetry = retryable && retryScheduler.CanScheduleAttempt(nextAttemptNumber, maxAttempts);
        var status = canRetry
            ? WebhookDeliveryAttemptStatus.Failed
            : WebhookDeliveryAttemptStatus.Abandoned;
        var nextRetryAt = canRetry
            ? retryScheduler.GetScheduledAtUtc(nextAttemptNumber, completedAt)
            : (DateTime?)null;

        await attemptRepository.MarkFailedAsync(
            attempt.TenantId,
            attempt.Id,
            leaseToken,
            status,
            completedAt,
            failureCategory,
            httpStatusCode,
            durationMs,
            responseBodyPreview,
            nextRetryAt,
            cancellationToken);

        await endpointRepository.MarkFailureAsync(attempt.TenantId, attempt.EndpointId, completedAt, cancellationToken);

        if (canRetry && nextRetryAt is { } scheduledAt)
        {
            await attemptRepository.CreateAsync(new WebhookDeliveryAttempt
            {
                Id = Guid.CreateVersion7(),
                TenantId = attempt.TenantId,
                MessageId = attempt.MessageId,
                EndpointId = attempt.EndpointId,
                AttemptNumber = nextAttemptNumber,
                Status = WebhookDeliveryAttemptStatus.Scheduled,
                ScheduledAt = scheduledAt,
                CreatedAt = completedAt
            }, cancellationToken);
        }
        else if (disableEndpointOnAbandon)
        {
            await endpointRepository.DisableAsync(
                attempt.TenantId,
                attempt.EndpointId,
                completedAt,
                cancellationToken);
            metrics.RecordWebhookEndpointDisabled(attempt.TenantId.ToString("D"), failureCategory);
        }

        await messageRepository.RefreshLocalDeliveryStatusAsync(
            attempt.TenantId,
            attempt.MessageId,
            completedAt,
            cancellationToken);

        var outcome = canRetry ? "retry_scheduled" : "abandoned";
        metrics.RecordWebhookDeliveryAttempt(
            attempt.TenantId.ToString("D"),
            attempt.Message?.EventType,
            outcome,
            failureCategory);
        metrics.RecordWebhookDeliveryFailure(
            attempt.TenantId.ToString("D"),
            attempt.Message?.EventType,
            outcome,
            failureCategory);

        if (canRetry)
        {
            logger.LogWarning(
                "Webhook delivery attempt {AttemptId} failed with {FailureCategory}; scheduled attempt {AttemptNumber}",
                attempt.Id,
                failureCategory,
                nextAttemptNumber);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.RetryScheduled, attempt.Id);
        }

        logger.LogWarning(
            "Webhook delivery attempt {AttemptId} abandoned with {FailureCategory}",
            attempt.Id,
            failureCategory);
        return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Abandoned, attempt.Id);
    }

    private Task<WebhookDeliverySingleDrainResult> AbandonAttemptAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        DateTime startedAt,
        string failureCategory,
        bool disableEndpoint,
        CancellationToken cancellationToken) =>
        MarkFailureAsync(
            attempt,
            leaseToken,
            startedAt,
            failureCategory,
            httpStatusCode: null,
            durationMs: 0,
            responseBodyPreview: null,
            retryable: false,
            disableEndpointOnAbandon: disableEndpoint,
            cancellationToken);

    private void RecordManualRetry(
        WebhookDeliveryAttempt attempt,
        string outcome,
        string? failureCategory = null)
    {
        metrics.RecordWebhookManualRetry(
            attempt.TenantId.ToString("D"),
            attempt.Message?.EventType,
            outcome,
            failureCategory);
    }

    private bool ShouldProcessLocalAttempts()
    {
        var options = webhookOptions.CurrentValue;
        return !options.IsDisabled
            && (options.IsProvider(WebhookOptions.ProviderLocal)
                || options.IsProvider(WebhookOptions.ProviderComposite));
    }

    private int GetMaxAttempts(WebhookEndpoint? endpoint)
    {
        var localMaxAttempts = webhookOptions.CurrentValue.Local.MaxAttempts;
        var endpointMaxAttempts = endpoint?.MaxAttempts > 0 ? endpoint.MaxAttempts : localMaxAttempts;
        return Math.Min(Math.Min(endpointMaxAttempts, localMaxAttempts), retryScheduler.MaxScheduledAttempts);
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status is >= 300 and <= 399;
    }

    private static async Task<string?> ReadResponsePreviewAsync(
        HttpContent? content,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        if (content is null || maxBytes <= 0)
        {
            return null;
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[maxBytes];
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        if (offset == 0)
        {
            return null;
        }

        return SanitizePreview(Encoding.UTF8.GetString(buffer, 0, offset));
    }

    private static string SanitizePreview(string preview)
    {
        var chars = new char[preview.Length];
        var index = 0;
        foreach (var current in preview)
        {
            if (!char.IsControl(current) || current is '\r' or '\n' or '\t')
            {
                chars[index++] = current;
            }
        }

        return new string(chars, 0, index).Trim();
    }

    private static int GetDurationMilliseconds(long startedTimestamp)
    {
        var elapsed = Stopwatch.GetElapsedTime(startedTimestamp);
        if (elapsed.TotalMilliseconds >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return Math.Max(0, (int)elapsed.TotalMilliseconds);
    }
}
