// ABOUTME: Internal outbox payload requesting provider synchronization for a submitted event report.
// ABOUTME: Contains safe report metadata only and intentionally excludes reporter text and raw hashes.

namespace Explore.Application.Models.InternalEvents;

public sealed record EventReportProviderSyncRequested
{
    public required Guid TenantId { get; init; }
    public required Guid ReportId { get; init; }
    public required Guid EventId { get; init; }
    public required Guid CaseId { get; init; }
    public required string ReasonCode { get; init; }
    public required string QueueCode { get; init; }
    public required DateTime SubmittedAtUtc { get; init; }
    public string? CorrelationId { get; init; }
}
