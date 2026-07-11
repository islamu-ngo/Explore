// ABOUTME: Input DTO for authenticated users submitting event reports.
// ABOUTME: Carries safe user-entered report metadata while server-derived hashes stay on the command.

using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventReporting;

public sealed class SubmitEventReportDto
{
    public Guid EventId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? SubcategoryCode { get; set; }
    public string ReporterText { get; set; } = string.Empty;
    public EventReportSeverityHint? SeverityHint { get; set; }
    public bool ReporterContactConsent { get; set; }
    public string? ReporterLocale { get; set; }
}
