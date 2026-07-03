// ABOUTME: Dispatches event-report provider sync outbox messages through the runtime provider boundary.
// ABOUTME: Persists idempotent external link and signal outcomes while keeping local reports authoritative.

using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventReporting.Models;
using Explore.Application.Models.InternalEvents;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Moderation;

public sealed class ReportProviderSyncDispatcher(
    IEventReportProvider provider,
    IEventReportRepository eventReportRepository,
    IOptionsMonitor<ModerationProviderOptions> options,
    BusinessMetrics metrics,
    ILogger<ReportProviderSyncDispatcher> logger) : IReportProviderSyncDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        if (message.EventType != EventReportOutboxMessageFactory.EventReportProviderSyncRequestedEventType)
        {
            throw new InvalidOperationException(
                $"Outbox message {message.Id} has unsupported event type '{message.EventType}' for report provider sync.");
        }

        var request = DeserializeRequest(message);
        var idempotencyKey = BuildIdempotencyKey(message);
        var currentOptions = options.CurrentValue;
        var configuredProviders = ResolveConfiguredProviders(currentOptions);
        var report = await LoadReportAsync(request, cancellationToken);
        var reportCase = ResolveCase(report, request.CaseId, message.Id);

        if (HasCompletedSyncMarker(report, configuredProviders, idempotencyKey))
        {
            RecordProviderSync(report.TenantId, configuredProviders, "skipped");
            logger.LogInformation(
                "Skipping already-recorded event report provider sync for report {ReportId} from outbox message {MessageId}",
                request.ReportId,
                message.Id);
            return;
        }

        var envelope = CreateEnvelope(request, report, reportCase, idempotencyKey, currentOptions);
        var result = await provider.SyncReportAsync(envelope, cancellationToken);

        if (result.ProviderDisabled)
        {
            await PersistDisabledAsync(report, reportCase.Id, configuredProviders, idempotencyKey, cancellationToken);
            RecordProviderSync(report.TenantId, configuredProviders, "disabled", "provider_disabled");
            logger.LogInformation(
                "Event report provider sync disabled for report {ReportId} from outbox message {MessageId}",
                request.ReportId,
                message.Id);
            return;
        }

        if (!result.Succeeded)
        {
            var category = NormalizeErrorCategory(result.Error?.Category);
            await PersistFailureAsync(report, reportCase.Id, configuredProviders, idempotencyKey, category, cancellationToken);
            RecordProviderSync(
                report.TenantId,
                configuredProviders,
                result.IsRetryable ? "retryable_failure" : "nonretryable_failure",
                category);
            logger.LogWarning(
                "Event report provider sync failed for report {ReportId} from outbox message {MessageId} with category {FailureCategory} retryable {IsRetryable}",
                request.ReportId,
                message.Id,
                category,
                result.IsRetryable);

            if (result.IsRetryable)
            {
                throw new InvalidOperationException($"event_report_provider_sync_failed:{category}");
            }

            return;
        }

        await PersistSuccessAsync(report, reportCase.Id, result, idempotencyKey, cancellationToken);
        RecordProviderSync(report.TenantId, configuredProviders, "succeeded");
        logger.LogInformation(
            "Event report provider sync succeeded for report {ReportId} from outbox message {MessageId}",
            request.ReportId,
            message.Id);
    }

    private static EventReportProviderSyncRequested DeserializeRequest(OutboxMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Payload))
        {
            throw new InvalidOperationException($"Outbox message {message.Id} has no payload for report provider sync.");
        }

        return JsonSerializer.Deserialize<EventReportProviderSyncRequested>(message.Payload, JsonOptions)
            ?? throw new JsonException($"Failed to deserialize report provider sync payload for message {message.Id}.");
    }

    private async Task<EventReport> LoadReportAsync(
        EventReportProviderSyncRequested request,
        CancellationToken cancellationToken)
    {
        var report = await eventReportRepository.GetByIdForUpdateAsync(
            request.TenantId,
            request.ReportId,
            cancellationToken);

        return report
            ?? throw new InvalidOperationException(
                $"Event report {request.ReportId} for tenant {request.TenantId} was not found for provider sync.");
    }

    private static EventReportCase ResolveCase(EventReport report, Guid caseId, Guid messageId)
    {
        return report.Cases.FirstOrDefault(reportCase => reportCase.Id == caseId)
            ?? throw new InvalidOperationException(
                $"Event report {report.Id} does not contain case {caseId} for provider sync message {messageId}.");
    }

    private static EventReportProviderEnvelope CreateEnvelope(
        EventReportProviderSyncRequested request,
        EventReport report,
        EventReportCase reportCase,
        string idempotencyKey,
        ModerationProviderOptions currentOptions) =>
        new(
            request.TenantId,
            request.ReportId,
            request.EventId,
            request.CaseId,
            report.ReasonCode,
            reportCase.QueueCode,
            ToCode(report.Status),
            ToCode(reportCase.Status),
            ToCode(reportCase.Priority),
            request.SubmittedAtUtc,
            Max(report.UpdatedAt, reportCase.UpdatedAt),
            idempotencyKey,
            NormalizeOptional(request.CorrelationId),
            currentOptions.EvidenceMode);

    private async Task PersistSuccessAsync(
        EventReport report,
        Guid caseId,
        EventReportProviderSyncResult result,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var changed = false;

        foreach (var signal in result.Signals)
        {
            if (HasSignal(report, signal))
            {
                continue;
            }

            report.Signals.Add(EventReportSignal.Create(
                report.TenantId,
                report.Id,
                report.EventId,
                signal.Provider,
                signal.SignalType,
                signal.PolicyCode,
                signal.Score,
                signal.Verdict,
                signal.RecommendedAction,
                signal.SafeSummary,
                signal.ExternalSignalId,
                string.IsNullOrWhiteSpace(signal.CorrelationId) ? idempotencyKey : signal.CorrelationId,
                signal.CreatedAtUtc == default ? utcNow : signal.CreatedAtUtc));
            changed = true;
        }

        foreach (var providerToMark in ResolveSuccessfulExternalProviders(result))
        {
            var link = GetOrCreateLink(report, caseId, providerToMark, idempotencyKey, utcNow);
            link.MarkSynced(
                providerToMark == EventReportExternalProvider.Coop ? result.ProviderCaseId : null,
                providerToMark == EventReportExternalProvider.Osprey ? result.ProviderSignalId : null,
                providerToMark == EventReportExternalProvider.Coop ? result.ProviderUrl : null,
                utcNow);
            changed = true;
        }

        if (changed)
        {
            await eventReportRepository.Update(report);
        }
    }

    private async Task PersistDisabledAsync(
        EventReport report,
        Guid caseId,
        IReadOnlyList<EventReportExternalProvider> providers,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (providers.Count == 0)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        foreach (var configuredProvider in providers)
        {
            GetOrCreateLink(report, caseId, configuredProvider, idempotencyKey, utcNow).Disable(utcNow);
        }

        await eventReportRepository.Update(report);
    }

    private async Task PersistFailureAsync(
        EventReport report,
        Guid caseId,
        IReadOnlyList<EventReportExternalProvider> providers,
        string idempotencyKey,
        string category,
        CancellationToken cancellationToken)
    {
        if (providers.Count == 0)
        {
            return;
        }

        var utcNow = DateTime.UtcNow;
        foreach (var configuredProvider in providers)
        {
            var link = GetOrCreateLink(report, caseId, configuredProvider, idempotencyKey, utcNow);
            if (link.SyncState == EventReportSyncState.Synced)
            {
                continue;
            }

            link.MarkFailed(category, utcNow);
        }

        await eventReportRepository.Update(report);
    }

    private static EventReportExternalLink GetOrCreateLink(
        EventReport report,
        Guid caseId,
        EventReportExternalProvider provider,
        string idempotencyKey,
        DateTime utcNow)
    {
        var existing = report.ExternalLinks.FirstOrDefault(link =>
            link.Provider == provider &&
            string.Equals(link.CorrelationId, idempotencyKey, StringComparison.Ordinal));

        if (existing is not null)
        {
            return existing;
        }

        var link = EventReportExternalLink.CreatePending(
            report.TenantId,
            report.Id,
            caseId,
            provider,
            idempotencyKey,
            utcNow);
        report.ExternalLinks.Add(link);
        return link;
    }

    private static bool HasCompletedSyncMarker(
        EventReport report,
        IReadOnlyList<EventReportExternalProvider> providers,
        string idempotencyKey)
    {
        return providers.Count > 0 && providers.All(providerToCheck =>
            report.ExternalLinks.Any(link =>
                link.Provider == providerToCheck &&
                string.Equals(link.CorrelationId, idempotencyKey, StringComparison.Ordinal) &&
                link.SyncState is EventReportSyncState.Synced or EventReportSyncState.Disabled or EventReportSyncState.Ignored));
    }

    private static bool HasSignal(EventReport report, EventSafetySignalEnvelope signal)
    {
        var externalSignalId = NormalizeOptional(signal.ExternalSignalId);
        if (!string.IsNullOrWhiteSpace(externalSignalId))
        {
            return report.Signals.Any(existing =>
                existing.Provider == signal.Provider &&
                string.Equals(existing.ExternalSignalId, externalSignalId, StringComparison.Ordinal));
        }

        var signalType = NormalizeRequired(signal.SignalType);
        var policyCode = NormalizeRequired(signal.PolicyCode);
        var correlationId = NormalizeRequired(signal.CorrelationId);

        return report.Signals.Any(existing =>
            existing.Provider == signal.Provider &&
            string.Equals(existing.SignalType, signalType, StringComparison.Ordinal) &&
            string.Equals(existing.PolicyCode, policyCode, StringComparison.Ordinal) &&
            string.Equals(existing.CorrelationId, correlationId, StringComparison.Ordinal));
    }

    private static IReadOnlyList<EventReportExternalProvider> ResolveConfiguredProviders(ModerationProviderOptions currentOptions)
    {
        List<EventReportExternalProvider> providers = [];

        if (currentOptions.UsesOsprey && currentOptions.EvaluateSignals)
        {
            providers.Add(EventReportExternalProvider.Osprey);
        }

        if (currentOptions.UsesCoop && currentOptions.MirrorReviewQueue)
        {
            providers.Add(EventReportExternalProvider.Coop);
        }

        return providers;
    }

    private static IReadOnlyList<EventReportExternalProvider> ResolveSuccessfulExternalProviders(EventReportProviderSyncResult result)
    {
        List<EventReportExternalProvider> providers = [];

        if (!string.IsNullOrWhiteSpace(result.ProviderCaseId) || !string.IsNullOrWhiteSpace(result.ProviderUrl))
        {
            providers.Add(EventReportExternalProvider.Coop);
        }

        if (!string.IsNullOrWhiteSpace(result.ProviderSignalId) ||
            result.Signals.Any(signal => signal.Provider == EventReportSignalProvider.Osprey))
        {
            providers.Add(EventReportExternalProvider.Osprey);
        }

        return providers;
    }

    private static string BuildIdempotencyKey(OutboxMessage message) =>
        $"event-report-provider-sync:{message.Id:N}";

    private void RecordProviderSync(
        Guid tenantId,
        IReadOnlyList<EventReportExternalProvider> providers,
        string outcome,
        string? failureCategory = null)
    {
        if (providers.Count == 0)
        {
            metrics.RecordEventReportProviderSync(tenantId.ToString(), "local", outcome, failureCategory);
            return;
        }

        foreach (var providerToRecord in providers)
        {
            metrics.RecordEventReportProviderSync(
                tenantId.ToString(),
                ToProviderCode(providerToRecord),
                outcome,
                failureCategory);
        }
    }

    private static string NormalizeErrorCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "provider_sync_failed" : category.Trim();

    private static string ToProviderCode(EventReportExternalProvider providerToRecord)
        => providerToRecord switch
        {
            EventReportExternalProvider.Osprey => "osprey",
            EventReportExternalProvider.Coop => "coop",
            _ => "unknown"
        };

    private static string NormalizeRequired(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? Max(DateTime? first, DateTime? second)
    {
        if (!first.HasValue)
        {
            return second;
        }

        if (!second.HasValue)
        {
            return first;
        }

        return first > second ? first : second;
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
}
