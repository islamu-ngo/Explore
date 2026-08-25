// ABOUTME: Input DTO for authenticated users submitting event reports.
// ABOUTME: Carries safe user-entered report metadata while server-derived hashes stay on the command.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventReporting;

public sealed record SubmitEventReportDto
{
    public Guid EventId { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string? SubcategoryCode { get; init; }
    public string ReporterText { get; init; } = string.Empty;
    public EventReportSeverityHint? SeverityHint { get; init; }
    public required bool ReportCaseUpdatesConsent { get; init; }
    public required bool ReportFollowUpContactConsent { get; init; }
    public string? ReporterLocale { get; init; }
}
