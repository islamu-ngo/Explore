// ABOUTME: MediatR command for local moderator triage of an event report case.
// ABOUTME: Uses event-level moderation authorization while handler verifies the report-event relationship.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ModerateLight)]
public sealed class TriageEventReportCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid ReportId { get; init; }
    public Guid CaseId { get; init; }
    public Guid ExpectedCaseConcurrencyStamp { get; init; }
    public required string QueueCode { get; init; }
    public EventReportPriority Priority { get; init; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
}
