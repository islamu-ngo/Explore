// ABOUTME: Public MediatR query for report dialog options for one event.
// ABOUTME: Lets the Application layer decide whether the event can currently be reported.

using Explore.Application.DTOs.EventReporting;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Queries;

public sealed record GetEventReportOptionsRequest : IRequest<EventReportOptionsDto?>
{
    public Guid EventId { get; init; }
}
