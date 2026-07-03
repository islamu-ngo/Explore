// ABOUTME: Management projection for provider mirror and synchronization state.
// ABOUTME: Includes bounded provider IDs, retry state, and safe error categories only.

namespace Explore.Application.DTOs.EventReporting;

public sealed class ModerationReportExternalLinkDto
{
    public Guid Id { get; init; }
    public Guid ReportId { get; init; }
    public Guid? CaseId { get; init; }
    public int ProviderId { get; init; }
    public required string ProviderCode { get; init; }
    public required string ProviderName { get; init; }
    public string? ProviderCaseId { get; init; }
    public string? ProviderSignalId { get; init; }
    public string? ProviderUrl { get; init; }
    public int SyncStateId { get; init; }
    public required string SyncStateCode { get; init; }
    public required string SyncStateName { get; init; }
    public DateTime? LastSyncedAtUtc { get; init; }
    public string? LastErrorCategory { get; init; }
    public int RetryCount { get; init; }
    public required string CorrelationId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
