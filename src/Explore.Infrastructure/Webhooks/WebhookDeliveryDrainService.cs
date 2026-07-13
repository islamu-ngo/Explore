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

        var claimedAt = DateTime.UtcNow;
        IReadOnlyList<WebhookDeliveryClaim> claims;
        int candidateCount;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
            var tenantOrder = await attemptRepository.GetDueTenantIdsAsync(
                _settings.CandidateBatchSize,
                claimedAt,
                cancellationToken);
            candidateCount = await attemptRepository.CountDueScheduledAsync(claimedAt, cancellationToken);
            if (tenantOrder.Count == 0)
            {
                claims = [];
            }
            else
            {
                var limits = tenantOrder.ToDictionary(
                    tenantId => tenantId,
                    _ => new WebhookDeliveryClaimLimits(
                        _settings.MaxConcurrentDeliveriesPerTenant,
                        _settings.MaxConcurrentDeliveriesPerEndpoint,
                        _settings.MaxItemsPerTenantPerClaimCycle));
                claims = await attemptRepository.ClaimDueAsync(
                    new WebhookDeliveryClaimRequest(
                        _settings.BatchSize,
                        _settings.CandidateBatchSize,
                        _settings.MaxConcurrentDeliveries,
                        tenantOrder,
                        claimedAt,
                        TimeSpan.FromSeconds(_settings.ProcessingLeaseTimeoutSeconds)),
                    limits,
                    cancellationToken);
            }
        }

        if (claims.Count == 0)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("No due webhook delivery attempts");
            }

            return new WebhookDeliveryDrainResult(0, 0, 0, 0, 0, 0, 0);
        }

        var results = await Task.WhenAll(claims.Select(claim =>
            SendClaimedAttemptAsync(
                claim.Attempt,
                claim.LeaseToken,
                claim.ProcessingFence,
                claim.ClaimedAt,
                cancellationToken)));
        var succeeded = results.Count(result => result.Outcome == WebhookDeliveryDrainOutcome.Succeeded);
        var retryScheduled = results.Count(result => result.Outcome == WebhookDeliveryDrainOutcome.RetryScheduled);
        var abandoned = results.Count(result => result.Outcome == WebhookDeliveryDrainOutcome.Abandoned);
        var alreadyClaimed = results.Count(result => result.Outcome == WebhookDeliveryDrainOutcome.AlreadyClaimed);
        var processed = succeeded + retryScheduled + abandoned;
        var skipped = results.Length - processed - alreadyClaimed;

        return new WebhookDeliveryDrainResult(
            candidateCount,
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
        WebhookDeliveryAttempt? attempt;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
            attempt = await attemptRepository.GetByTenantAndIdAsync(tenantId, attemptId, cancellationToken);
        }
        if (attempt is null)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Missing);
        }

        if (attempt.Outcome is WebhookDeliveryAttemptOutcome.Succeeded or WebhookDeliveryAttemptOutcome.Abandoned)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadySettled, attempt.Id);
        }

        if (attempt.Outcome == WebhookDeliveryAttemptOutcome.Sending)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        if (attempt.Outcome == WebhookDeliveryAttemptOutcome.Scheduled && attempt.ScheduledAt > DateTime.UtcNow)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Deferred, attempt.Id);
        }

        if (!ShouldProcessLocalAttempts())
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Skipped, attempt.Id);
        }

        if (attempt.Outcome != WebhookDeliveryAttemptOutcome.Scheduled)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Skipped, attempt.Id);
        }

        var claims = await ClaimSingleAsync(tenantId, attemptId, cancellationToken);
        if (claims.Count == 0)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        var claim = claims.Single();
        return await SendClaimedAttemptAsync(
            claim.Attempt,
            claim.LeaseToken,
            claim.ProcessingFence,
            claim.ClaimedAt,
            cancellationToken);
    }

    public async Task<WebhookDeliverySingleDrainResult> ScheduleManualRetryAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var attempt = await attemptRepository.GetByTenantAndIdAsync(tenantId, attemptId, cancellationToken);
        if (attempt is null)
        {
            metrics.RecordWebhookManualRetry(
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

        if (attempt.Outcome == WebhookDeliveryAttemptOutcome.Scheduled)
        {
            RecordManualRetry(attempt, "deferred", AttemptStatusNotRetryableFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Deferred, attempt.Id);
        }

        if (attempt.Outcome == WebhookDeliveryAttemptOutcome.Sending)
        {
            RecordManualRetry(attempt, "already_claimed", AttemptStatusNotRetryableFailureCategory);
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        if (attempt.Outcome == WebhookDeliveryAttemptOutcome.Succeeded)
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

        if (attempt.Message.GetPayloadBytes() is null)
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
            Outcome = WebhookDeliveryAttemptOutcome.Scheduled,
            ScheduledAt = now,
            CreatedAt = now
        }, cancellationToken);

        metrics.RecordWebhookManualRetry(
            eventType,
            "retry_scheduled");
        return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.RetryScheduled, retry.Id);
    }

    private async Task<IReadOnlyList<WebhookDeliveryClaim>> ClaimSingleAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var claimedAt = DateTime.UtcNow;
        return await attemptRepository.ClaimDueAsync(
            new WebhookDeliveryClaimRequest(
                1,
                Math.Max(1, _settings.CandidateBatchSize),
                _settings.MaxConcurrentDeliveries,
                [tenantId],
                claimedAt,
                TimeSpan.FromSeconds(_settings.ProcessingLeaseTimeoutSeconds),
                attemptId),
            new Dictionary<Guid, WebhookDeliveryClaimLimits>
            {
                [tenantId] = new(
                    _settings.MaxConcurrentDeliveriesPerTenant,
                    _settings.MaxConcurrentDeliveriesPerEndpoint,
                    1)
            },
            cancellationToken);
    }

    private async Task<WebhookDeliverySingleDrainResult> SendClaimedAttemptAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        long processingFence,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        await using var executionScope = scopeFactory.CreateAsyncScope();
        var tenantContextAccessor = executionScope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        tenantContextAccessor.SetTenant(attempt.TenantId);

        if (attempt.Endpoint is null)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                processingFence,
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
                processingFence,
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
                processingFence,
                startedAt,
                EndpointNotActiveFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        var payloadBytes = attempt.Message.GetPayloadBytes();
        if (payloadBytes is null)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                processingFence,
                startedAt,
                PayloadUnavailableFailureCategory,
                disableEndpoint: false,
                cancellationToken);
        }

        var localOptions = webhookOptions.CurrentValue.Local;
        if (payloadBytes.Length > localOptions.MaxPayloadBytes)
        {
            return await AbandonAttemptAsync(
                attempt,
                leaseToken,
                processingFence,
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
                processingFence,
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
                processingFence,
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
                processingFence,
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
                ? Math.Min(attempt.Endpoint.TimeoutSeconds, localOptions.TimeoutSeconds)
                : localOptions.TimeoutSeconds;
            requestTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var request = BuildRequest(endpointUri, attempt.Message.Id, payloadBytes, secret);
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
                    processingFence,
                    startedAt,
                    completedAt,
                    (int)response.StatusCode,
                    durationMs,
                    cancellationToken);
            }

            var failureCategory = IsRedirect(response.StatusCode)
                ? RedirectResponseFailureCategory
                : HttpNonSuccessFailureCategory;
            var retryAfter = GetRetryAfter(response.Headers.RetryAfter, completedAt);
            return await MarkFailureAsync(
                attempt,
                leaseToken,
                processingFence,
                completedAt,
                failureCategory,
                (int)response.StatusCode,
                durationMs,
                retryable: true,
                disableEndpointOnAbandon: true,
                cancellationToken,
                retryAfter);
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
                processingFence,
                DateTime.UtcNow,
                TimeoutFailureCategory,
                httpStatusCode: null,
                GetDurationMilliseconds(startedTimestamp),
                retryable: true,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return await MarkFailureAsync(
                attempt,
                leaseToken,
                processingFence,
                DateTime.UtcNow,
                NetworkFailureCategory,
                httpStatusCode: null,
                GetDurationMilliseconds(startedTimestamp),
                retryable: true,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
        catch (FormatException)
        {
            return await MarkFailureAsync(
                attempt,
                leaseToken,
                processingFence,
                DateTime.UtcNow,
                InvalidSecretFailureCategory,
                httpStatusCode: null,
                GetDurationMilliseconds(startedTimestamp),
                retryable: false,
                disableEndpointOnAbandon: true,
                cancellationToken);
        }
    }

    private HttpRequestMessage BuildRequest(
        Uri endpointUri,
        Guid messageId,
        byte[] payloadBytes,
        WebhookSecretMaterial secret)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var signatureHeaders = signatureService.Sign(messageId.ToString("N"), timestamp, payloadBytes, secret);

        var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
        {
            Content = new ByteArrayContent(payloadBytes)
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        request.Headers.TryAddWithoutValidation("svix-id", signatureHeaders.SvixId);
        request.Headers.TryAddWithoutValidation("svix-timestamp", signatureHeaders.SvixTimestamp);
        request.Headers.TryAddWithoutValidation("svix-signature", signatureHeaders.SvixSignature);
        return request;
    }

    private async Task<WebhookDeliverySingleDrainResult> MarkSucceededAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        long processingFence,
        DateTime startedAt,
        DateTime completedAt,
        int httpStatusCode,
        int durationMs,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var endpointRepository = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();

        var settled = await attemptRepository.MarkSucceededAsync(
            attempt.TenantId,
            attempt.Id,
            leaseToken,
            processingFence,
            startedAt,
            completedAt,
            httpStatusCode,
            durationMs,
            cancellationToken);

        if (!settled)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        await endpointRepository.MarkSuccessAsync(
            attempt.TenantId,
            attempt.EndpointId,
            completedAt,
            cancellationToken);

        metrics.RecordWebhookDeliveryAttempt(
            attempt.Message?.EventType,
            "succeeded");
        metrics.RecordWebhookDeliverySuccess(
            attempt.Message?.EventType);

        return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.Succeeded, attempt.Id);
    }

    private async Task<WebhookDeliverySingleDrainResult> MarkFailureAsync(
        WebhookDeliveryAttempt attempt,
        Guid leaseToken,
        long processingFence,
        DateTime completedAt,
        string failureCategory,
        int? httpStatusCode,
        int durationMs,
        bool retryable,
        bool disableEndpointOnAbandon,
        CancellationToken cancellationToken,
        TimeSpan? retryAfter = null)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var attemptRepository = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryAttemptRepository>();
        var endpointRepository = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var maxAttempts = GetMaxAttempts(attempt.Endpoint);
        var autoPauseThreshold = disableEndpointOnAbandon && !retryable ? 1 : maxAttempts;
        var nextAttemptNumber = attempt.AttemptNumber + 1;
        var canRetry = retryable && retryScheduler.CanScheduleAttempt(nextAttemptNumber, maxAttempts);
        var status = canRetry
            ? WebhookDeliveryAttemptOutcome.Failed
            : WebhookDeliveryAttemptOutcome.Abandoned;
        var nextRetryAt = canRetry
            ? retryScheduler.GetScheduledAtUtc(nextAttemptNumber, completedAt, retryAfter)
            : (DateTime?)null;

        var settled = await attemptRepository.MarkFailedAsync(
            attempt.TenantId,
            attempt.Id,
            leaseToken,
            processingFence,
            status,
            completedAt,
            failureCategory,
            httpStatusCode,
            durationMs,
            nextRetryAt,
            cancellationToken);

        if (!settled)
        {
            return new WebhookDeliverySingleDrainResult(WebhookDeliveryDrainOutcome.AlreadyClaimed, attempt.Id);
        }

        var failureState = await endpointRepository.RecordFailureAsync(
            attempt.TenantId,
            attempt.EndpointId,
            completedAt,
            failureCategory,
            autoPauseThreshold,
            cancellationToken);
        canRetry = canRetry && !failureState.IsAutoPaused;

        if (canRetry && nextRetryAt is { } scheduledAt)
        {
            await attemptRepository.CreateAsync(new WebhookDeliveryAttempt
            {
                Id = Guid.CreateVersion7(),
                TenantId = attempt.TenantId,
                MessageId = attempt.MessageId,
                EndpointId = attempt.EndpointId,
                AttemptNumber = nextAttemptNumber,
                Outcome = WebhookDeliveryAttemptOutcome.Scheduled,
                ScheduledAt = scheduledAt,
                CreatedAt = completedAt
            }, cancellationToken);
        }
        else if (failureState.IsAutoPaused)
        {
            metrics.RecordWebhookEndpointDisabled(failureCategory);
        }

        var outcome = canRetry ? "retry_scheduled" : "abandoned";
        metrics.RecordWebhookDeliveryAttempt(
            attempt.Message?.EventType,
            outcome,
            failureCategory);
        metrics.RecordWebhookDeliveryFailure(
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
        long processingFence,
        DateTime startedAt,
        string failureCategory,
        bool disableEndpoint,
        CancellationToken cancellationToken) =>
        MarkFailureAsync(
            attempt,
            leaseToken,
            processingFence,
            startedAt,
            failureCategory,
            httpStatusCode: null,
            durationMs: 0,
            retryable: false,
            disableEndpointOnAbandon: disableEndpoint,
            cancellationToken);

    private void RecordManualRetry(
        WebhookDeliveryAttempt attempt,
        string outcome,
        string? failureCategory = null)
    {
        metrics.RecordWebhookManualRetry(
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

    private TimeSpan? GetRetryAfter(
        System.Net.Http.Headers.RetryConditionHeaderValue? header,
        DateTime responseReceivedAt)
    {
        if (header is null || webhookOptions.CurrentValue.Local.MaxRetryAfterSeconds == 0)
        {
            return null;
        }

        var delay = header.Delta
            ?? (header.Date is { } retryAt
                ? retryAt - new DateTimeOffset(DateTime.SpecifyKind(responseReceivedAt, DateTimeKind.Utc))
                : null);
        return delay is { } value && value > TimeSpan.Zero ? value : null;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var status = (int)statusCode;
        return status is >= 300 and <= 399;
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
