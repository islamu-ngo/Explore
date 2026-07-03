// ABOUTME: Secured MediatR query for one event-report management detail projection.
// ABOUTME: Requires the caller to authorize against the report's concrete event before evidence is loaded.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventReporting;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetModerationReportDetailRequest : IRequest<ModerationReportDetailDto?>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid ReportId { get; init; }

    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
}
