// ABOUTME: MediatR query request for dedicated event session/program item creation context.
// ABOUTME: Returns server-owned defaults and selector options for an event-scoped composer.

using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

public class GetEventSessionCreateContextRequest : IRequest<EventSessionCreateContextDto?>
{
    public Guid EventId { get; set; }
}
