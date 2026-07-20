// ABOUTME: Reporter-owned request for changing event-report communication consent.
// ABOUTME: Keeps case-update and follow-up contact purposes explicit and independent.

namespace Explore.Application.DTOs.EventReporting;

public sealed class UpdateMyReportCommunicationConsentDto
{
    public required bool ReportCaseUpdatesConsent { get; init; }
    public required bool ReportFollowUpContactConsent { get; init; }
}
