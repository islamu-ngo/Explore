// ABOUTME: Grouped PATCH contract for reporter-owned communication consent.
// ABOUTME: Keeps both communication choices explicit and isolated from report content.

namespace Explore.Application.DTOs.EventReporting;

public sealed class UpdateMyReportCommunicationConsentDto
{
    public required ReportCommunicationConsentUpdateDto Consent { get; set; }
}

public sealed class ReportCommunicationConsentUpdateDto
{
    public bool ReportCaseUpdatesConsent { get; set; }
    public bool ReportFollowUpContactConsent { get; set; }
}
