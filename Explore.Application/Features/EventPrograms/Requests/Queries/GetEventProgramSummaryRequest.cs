// ABOUTME: Query contract for the server-backed event program summary.
// ABOUTME: Keeps program grouping/readiness rules in Application instead of Blazor shells.

using Explore.Application.DTOs.EventProgram;
using MediatR;

namespace Explore.Application.Features.EventPrograms.Requests.Queries;

public class GetEventProgramSummaryRequest : IRequest<EventProgramSummaryDto?>
{
    public Guid EventId { get; set; }
}
