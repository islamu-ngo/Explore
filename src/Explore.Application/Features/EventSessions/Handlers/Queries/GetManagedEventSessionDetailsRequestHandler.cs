// ABOUTME: Handles exact organizer-facing session detail reads without public location redaction.
// ABOUTME: Verifies the session belongs to the event used for resource authorization before mapping.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries;

public sealed class GetManagedEventSessionDetailsRequestHandler(
    IEventSessionRepository eventSessionRepository,
    IMapper mapper)
    : IRequestHandler<GetManagedEventSessionDetailsRequest, EventSessionDto?>
{
    public async Task<EventSessionDto?> Handle(
        GetManagedEventSessionDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var session = await eventSessionRepository.GetSessionWithDetails(request.Id);
        return session?.EventId == request.EventId
            ? mapper.Map<EventSessionDto>(session)
            : null;
    }
}
