// ABOUTME: Osprey moderation signal provider adapter over a configurable HTTP JSON evaluation endpoint.
// ABOUTME: Maps local report metadata into safe Osprey requests and normalizes returned signals.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.EventReporting.Models;
using Explore.Domain.Enums;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Moderation;

public sealed class OspreyModerationSignalProvider(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OspreyProviderOptions> options,
    ILogger<OspreyModerationSignalProvider> logger) : IModerationSignalProvider
{
    public const string HttpClientName = "OspreyModerationClient";

    private const int MaxSignalTypeLength = 100;
    private const int MaxPolicyCodeLength = 100;
    private const int MaxSafeSummaryLength = 500;
    private const int MaxExternalSignalIdLength = 200;
    private const int MaxCorrelationIdLength = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EventSafetySignalProviderResult> EvaluateAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentOptions = options.CurrentValue;
        if (!IsConfigured(currentOptions))
        {
            return EventSafetySignalProviderResult.Failure(
                "osprey_provider_disabled",
                isTransient: false,
                "Osprey signal provider is not enabled or configured.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(currentOptions.TimeoutSeconds, 1, 300)));

        try
        {
            using var request = CreateRequest(currentOptions, CreatePayload(envelope, currentOptions));
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var failure = MapHttpFailure(response.StatusCode);
                logger.LogWarning(
                    "Osprey signal evaluation failed with status {StatusCode} and category {FailureCategory}",
                    (int)response.StatusCode,
                    failure.Category);
                return EventSafetySignalProviderResult.Failure(
                    failure.Category,
                    failure.IsTransient,
                    failure.SafeDetail);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            var providerResponse = await JsonSerializer.DeserializeAsync<OspreyEvaluateResponse>(
                stream,
                JsonOptions,
                timeoutCts.Token);

            if (providerResponse is null)
            {
                return EventSafetySignalProviderResult.Failure(
                    "osprey_invalid_response",
                    isTransient: false,
                    "Osprey returned an empty or invalid response.");
            }

            return EventSafetySignalProviderResult.Success(MapSignals(envelope, providerResponse));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return EventSafetySignalProviderResult.Failure(
                "osprey_timeout",
                isTransient: true,
                "Osprey signal evaluation timed out.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Osprey signal evaluation request failed with {FailureType}", ex.GetType().Name);
            return EventSafetySignalProviderResult.Failure(
                "osprey_unreachable",
                isTransient: true,
                ex.GetType().Name);
        }
        catch (JsonException)
        {
            return EventSafetySignalProviderResult.Failure(
                "osprey_invalid_response",
                isTransient: false,
                "Osprey returned a response that could not be parsed.");
        }
        catch (UriFormatException)
        {
            return EventSafetySignalProviderResult.Failure(
                "osprey_invalid_configuration",
                isTransient: false,
                "Osprey endpoint configuration is invalid.");
        }
    }

    private static bool IsConfigured(OspreyProviderOptions currentOptions) =>
        currentOptions.Enabled && !string.IsNullOrWhiteSpace(currentOptions.EndpointUrl);

    private static OspreyEvaluateRequest CreatePayload(
        EventReportProviderEnvelope envelope,
        OspreyProviderOptions currentOptions) =>
        new()
        {
            EventType = NormalizeRequired(currentOptions.EventType, "event_report", MaxSignalTypeLength),
            TenantId = envelope.TenantId,
            ReportId = envelope.ReportId,
            EventId = envelope.EventId,
            CaseId = envelope.CaseId,
            ReasonCode = envelope.ReasonCode,
            QueueCode = envelope.QueueCode,
            ReportStatusCode = envelope.ReportStatusCode,
            CaseStatusCode = envelope.CaseStatusCode,
            PriorityCode = envelope.PriorityCode,
            SubmittedAtUtc = envelope.SubmittedAtUtc,
            LastUpdatedAtUtc = envelope.LastUpdatedAtUtc,
            IdempotencyKey = envelope.IdempotencyKey,
            CorrelationId = envelope.CorrelationId,
            EvidenceMode = ToCode(envelope.EvidenceMode)
        };

    private static HttpRequestMessage CreateRequest(
        OspreyProviderOptions currentOptions,
        OspreyEvaluateRequest payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuildEvaluateUri(currentOptions));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(currentOptions.ApiKey))
        {
            var headerName = currentOptions.ApiKeyHeaderName.Trim();
            if (string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentOptions.ApiKey.Trim());
            }
            else
            {
                request.Headers.TryAddWithoutValidation(headerName, currentOptions.ApiKey.Trim());
            }
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return request;
    }

    private static Uri BuildEvaluateUri(OspreyProviderOptions currentOptions)
    {
        var endpoint = new Uri(currentOptions.EndpointUrl.Trim(), UriKind.Absolute);
        var endpointPath = endpoint.AbsolutePath.TrimEnd('/');
        var evaluatePath = currentOptions.EvaluatePath.Trim();

        if (endpointPath.EndsWith(evaluatePath, StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        var builder = new UriBuilder(endpoint)
        {
            Path = string.IsNullOrEmpty(endpointPath)
                ? evaluatePath.TrimStart('/')
                : $"{endpointPath}/{evaluatePath.TrimStart('/')}",
            Query = string.Empty
        };

        return builder.Uri;
    }

    private static OspreyFailure MapHttpFailure(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity =>
                new("osprey_invalid_request", false, "Osprey rejected the signal evaluation request."),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new("osprey_auth_failed", false, "Osprey rejected the configured credentials."),
            HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests =>
                new("osprey_transient_http_failure", true, $"HTTP {(int)statusCode}"),
            >= HttpStatusCode.InternalServerError =>
                new("osprey_transient_http_failure", true, $"HTTP {(int)statusCode}"),
            _ => new("osprey_http_failure", false, $"HTTP {(int)statusCode}")
        };
    }

    private static IReadOnlyList<EventSafetySignalEnvelope> MapSignals(
        EventReportProviderEnvelope envelope,
        OspreyEvaluateResponse providerResponse)
    {
        var sourceSignals = providerResponse.Signals is { Count: > 0 }
            ? providerResponse.Signals
            : CreateSingleSignal(providerResponse);

        if (sourceSignals.Count == 0)
        {
            return [];
        }

        var utcNow = DateTime.UtcNow;
        return sourceSignals.Select(signal => new EventSafetySignalEnvelope(
                envelope.TenantId,
                envelope.ReportId,
                envelope.EventId,
                EventReportSignalProvider.Osprey,
                NormalizeRequired(signal.SignalType, "osprey_signal", MaxSignalTypeLength),
                NormalizeRequired(signal.PolicyCode, "osprey.policy", MaxPolicyCodeLength),
                signal.Score,
                MapVerdict(signal.Verdict),
                MapRecommendedAction(signal.RecommendedAction),
                NormalizeOptional(signal.SafeSummary, MaxSafeSummaryLength),
                NormalizeOptional(signal.ExternalSignalId, MaxExternalSignalIdLength),
                ResolveCorrelationId(signal.CorrelationId, providerResponse.CorrelationId, envelope),
                signal.CreatedAtUtc?.UtcDateTime ?? utcNow))
            .ToList();
    }

    private static IReadOnlyList<OspreySignalResponse> CreateSingleSignal(OspreyEvaluateResponse providerResponse)
    {
        if (string.IsNullOrWhiteSpace(providerResponse.SignalType)
            && string.IsNullOrWhiteSpace(providerResponse.PolicyCode)
            && string.IsNullOrWhiteSpace(providerResponse.Verdict)
            && string.IsNullOrWhiteSpace(providerResponse.ExternalSignalId))
        {
            return [];
        }

        return
        [
            new OspreySignalResponse
            {
                SignalType = providerResponse.SignalType,
                PolicyCode = providerResponse.PolicyCode,
                Score = providerResponse.Score,
                Verdict = providerResponse.Verdict,
                RecommendedAction = providerResponse.RecommendedAction,
                SafeSummary = providerResponse.SafeSummary,
                ExternalSignalId = providerResponse.ExternalSignalId,
                CorrelationId = providerResponse.CorrelationId,
                CreatedAtUtc = providerResponse.CreatedAtUtc
            }
        ];
    }

    private static EventReportSignalVerdict MapVerdict(string? value)
    {
        return NormalizeCode(value) switch
        {
            "no_signal" or "none" or "allow" or "ok" => EventReportSignalVerdict.NoSignal,
            "likely_violation" or "violation" or "match" or "matched" => EventReportSignalVerdict.LikelyViolation,
            "urgent" or "critical" or "high_risk" => EventReportSignalVerdict.Urgent,
            "auto_action_recommended" or "auto_action" or "action" => EventReportSignalVerdict.AutoActionRecommended,
            _ => EventReportSignalVerdict.NeedsReview
        };
    }

    private static EventReportRecommendedAction? MapRecommendedAction(string? value)
    {
        return NormalizeCode(value) switch
        {
            "" => null,
            "none" or "no_action" => EventReportRecommendedAction.None,
            "dismiss" => EventReportRecommendedAction.Dismiss,
            "light_moderate" or "light_moderation" => EventReportRecommendedAction.LightModerate,
            "heavy_redact" or "heavy_redaction" => EventReportRecommendedAction.HeavyRedact,
            "escalate" or "escalation" => EventReportRecommendedAction.Escalate,
            _ => null
        };
    }

    private static string ResolveCorrelationId(
        string? signalCorrelationId,
        string? responseCorrelationId,
        EventReportProviderEnvelope envelope) =>
        NormalizeOptional(signalCorrelationId, MaxCorrelationIdLength)
        ?? NormalizeOptional(responseCorrelationId, MaxCorrelationIdLength)
        ?? NormalizeOptional(envelope.CorrelationId, MaxCorrelationIdLength)
        ?? NormalizeRequired(envelope.IdempotencyKey, "osprey-correlation", MaxCorrelationIdLength);

    private static string NormalizeRequired(string? value, string fallback, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Replace('-', '_').ToLowerInvariant();
    }

    private static string ToCode<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (i > 0 && char.IsUpper(character))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private sealed record OspreyFailure(string Category, bool IsTransient, string SafeDetail);

    private sealed class OspreyEvaluateRequest
    {
        [JsonPropertyName("event_type")]
        public required string EventType { get; init; }

        [JsonPropertyName("tenant_id")]
        public Guid TenantId { get; init; }

        [JsonPropertyName("report_id")]
        public Guid ReportId { get; init; }

        [JsonPropertyName("event_id")]
        public Guid EventId { get; init; }

        [JsonPropertyName("case_id")]
        public Guid CaseId { get; init; }

        [JsonPropertyName("reason_code")]
        public required string ReasonCode { get; init; }

        [JsonPropertyName("queue_code")]
        public required string QueueCode { get; init; }

        [JsonPropertyName("report_status")]
        public required string ReportStatusCode { get; init; }

        [JsonPropertyName("case_status")]
        public required string CaseStatusCode { get; init; }

        [JsonPropertyName("priority")]
        public required string PriorityCode { get; init; }

        [JsonPropertyName("submitted_at_utc")]
        public DateTime SubmittedAtUtc { get; init; }

        [JsonPropertyName("last_updated_at_utc")]
        public DateTime? LastUpdatedAtUtc { get; init; }

        [JsonPropertyName("idempotency_key")]
        public required string IdempotencyKey { get; init; }

        [JsonPropertyName("correlation_id")]
        public string? CorrelationId { get; init; }

        [JsonPropertyName("evidence_mode")]
        public required string EvidenceMode { get; init; }
    }

    private sealed class OspreyEvaluateResponse
    {
        [JsonPropertyName("signals")]
        public IReadOnlyList<OspreySignalResponse>? Signals { get; init; }

        [JsonPropertyName("signal_type")]
        public string? SignalType { get; init; }

        [JsonPropertyName("policy_code")]
        public string? PolicyCode { get; init; }

        [JsonPropertyName("score")]
        public decimal? Score { get; init; }

        [JsonPropertyName("verdict")]
        public string? Verdict { get; init; }

        [JsonPropertyName("recommended_action")]
        public string? RecommendedAction { get; init; }

        [JsonPropertyName("safe_summary")]
        public string? SafeSummary { get; init; }

        [JsonPropertyName("external_signal_id")]
        public string? ExternalSignalId { get; init; }

        [JsonPropertyName("correlation_id")]
        public string? CorrelationId { get; init; }

        [JsonPropertyName("created_at_utc")]
        public DateTimeOffset? CreatedAtUtc { get; init; }
    }

    private sealed class OspreySignalResponse
    {
        [JsonPropertyName("signal_type")]
        public string? SignalType { get; init; }

        [JsonPropertyName("policy_code")]
        public string? PolicyCode { get; init; }

        [JsonPropertyName("score")]
        public decimal? Score { get; init; }

        [JsonPropertyName("verdict")]
        public string? Verdict { get; init; }

        [JsonPropertyName("recommended_action")]
        public string? RecommendedAction { get; init; }

        [JsonPropertyName("safe_summary")]
        public string? SafeSummary { get; init; }

        [JsonPropertyName("external_signal_id")]
        public string? ExternalSignalId { get; init; }

        [JsonPropertyName("correlation_id")]
        public string? CorrelationId { get; init; }

        [JsonPropertyName("created_at_utc")]
        public DateTimeOffset? CreatedAtUtc { get; init; }
    }
}
