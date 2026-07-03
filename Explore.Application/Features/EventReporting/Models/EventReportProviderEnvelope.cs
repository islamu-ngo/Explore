// ABOUTME: Provider-neutral envelope for synchronizing event reports to moderation integrations.
// ABOUTME: Carries stable IDs and safe metadata only; encrypted evidence and reporter hashes stay local.

namespace Explore.Application.Features.EventReporting.Models;

public enum EventReportProviderEvidenceMode
{
    MetadataOnly = 1,
    SafeSummaryOnly = 2,
    ReporterText = 3
}

public sealed record EventReportProviderEnvelope(
    Guid TenantId,
    Guid ReportId,
    Guid EventId,
    Guid CaseId,
    string ReasonCode,
    string QueueCode,
    string ReportStatusCode,
    string CaseStatusCode,
    string PriorityCode,
    DateTime SubmittedAtUtc,
    DateTime? LastUpdatedAtUtc,
    string IdempotencyKey,
    string? CorrelationId,
    EventReportProviderEvidenceMode EvidenceMode = EventReportProviderEvidenceMode.MetadataOnly);

public sealed record EventReportProviderError(
    string Category,
    bool IsTransient,
    string? SafeDetail = null);

public sealed record EventReportProviderSyncResult(
    bool Succeeded,
    bool ProviderDisabled,
    bool IsRetryable,
    string? ProviderCaseId,
    string? ProviderSignalId,
    string? ProviderUrl,
    IReadOnlyList<EventSafetySignalEnvelope> Signals,
    EventReportProviderError? Error)
{
    public static EventReportProviderSyncResult Success(
        string? providerCaseId = null,
        string? providerSignalId = null,
        string? providerUrl = null,
        IReadOnlyList<EventSafetySignalEnvelope>? signals = null) =>
        new(true, false, false, providerCaseId, providerSignalId, providerUrl, signals ?? [], null);

    public static EventReportProviderSyncResult Disabled(string? safeDetail = null) =>
        new(false, true, false, null, null, null, [], new EventReportProviderError("provider_disabled", false, safeDetail));

    public static EventReportProviderSyncResult Failure(
        string category,
        bool isTransient,
        string? safeDetail = null) =>
        new(false, false, isTransient, null, null, null, [], new EventReportProviderError(category, isTransient, safeDetail));
}
