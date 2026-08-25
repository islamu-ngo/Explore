// ABOUTME: Grouped PATCH contract for reporter-owned communication consent.
// ABOUTME: Keeps both communication choices explicit and isolated from report content.

namespace Explore.Application.DTOs.EventReporting;

public sealed record UpdateMyReportCommunicationConsentDto
{
    public required ReportCommunicationConsentUpdateDto Consent { get; init; }
}

public sealed record ReportCommunicationConsentUpdateDto
{
    public bool ReportCaseUpdatesConsent { get; init; }
    public bool ReportFollowUpContactConsent { get; init; }
}
