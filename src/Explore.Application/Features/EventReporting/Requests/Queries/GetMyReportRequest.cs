// ABOUTME: Authenticated MediatR query for a reporter's own event-report status.
// ABOUTME: Handler enforces ownership by current user before returning any report metadata.

using Explore.Application.DTOs.EventReporting;
using MediatR;

namespace Explore.Application.Features.EventReporting.Requests.Queries;

public sealed class GetMyReportRequest : IRequest<MyEventReportDto?>
{
    public Guid ReportId { get; init; }
}
