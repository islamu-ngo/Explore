// ABOUTME: Client contract for moderator-facing event-report queue and detail reads.
// ABOUTME: Keeps privileged moderation evidence behind an explicit service boundary.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.EventReporting;

public interface IEventReportModerationService
{
    Task<ModerationReportQueuePageResult> GetQueueAsync(
        Guid eventId,
        ModerationReportQueueQueryState query,
        CancellationToken cancellationToken = default);

    Task<HalResourceOfModerationReportDetailDto?> GetDetailAsync(
        Guid eventId,
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<ModerationReportActionResult> TriageAsync(
        Guid eventId,
        Guid reportId,
        ModerationReportTriageActionRequest request,
        CancellationToken cancellationToken = default);

    Task<ModerationReportActionResult> AssignAsync(
        Guid eventId,
        Guid reportId,
        ModerationReportAssignActionRequest request,
        CancellationToken cancellationToken = default);

    Task<ModerationReportActionResult> DecideAsync(
        Guid eventId,
        Guid reportId,
        ModerationReportDecisionActionRequest request,
        CancellationToken cancellationToken = default);

    Task<ModerationReportActionResult> ExecuteDecisionAsync(
        Guid eventId,
        Guid reportId,
        ModerationReportExecuteDecisionActionRequest request,
        CancellationToken cancellationToken = default);
}
