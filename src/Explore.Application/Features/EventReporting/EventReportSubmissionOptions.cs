// ABOUTME: Configurable limits and defaults for event-report submission handling.
// ABOUTME: Binds from the Reporting configuration section used by local-first moderation intake.

namespace Explore.Application.Features.EventReporting;

public sealed class EventReportSubmissionOptions
{
    public const string SectionName = "Reporting";

    public bool RequireAuthenticatedReporter { get; set; } = true;
    public int MaxReportsPerUserPerHour { get; set; } = 10;
    public int MaxReportsPerEventPerUserPerDay { get; set; } = 3;
    public int DuplicateWindowHours { get; set; } = 24;
    public int ReporterTextRetentionDays { get; set; } = 180;
    public int MaxReporterTextLength { get; set; } = 4000;
    public string DefaultQueueCode { get; set; } = "default";
    public int CaseSlaHours { get; set; } = 48;
    public string? ReporterFingerprintPepper { get; set; }
}
