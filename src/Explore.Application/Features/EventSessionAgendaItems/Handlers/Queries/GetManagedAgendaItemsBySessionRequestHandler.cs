// ABOUTME: Handles exact organizer-facing session agenda reads without public location redaction.
// ABOUTME: Verifies the selected session belongs to the event used for resource authorization.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Queries;

public sealed class GetManagedAgendaItemsBySessionRequestHandler(
    IEventSessionRepository sessionRepository,
    IEventSessionAgendaItemRepository agendaItemRepository,
    IMapper mapper)
    : IRequestHandler<GetManagedAgendaItemsBySessionRequest, List<EventSessionAgendaItemListDto>?>
{
    public async Task<List<EventSessionAgendaItemListDto>?> Handle(
        GetManagedAgendaItemsBySessionRequest request,
        CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetSessionWithDetails(request.EventSessionId);
        if (session?.EventId != request.EventId)
            return null;

        var items = await agendaItemRepository.GetBySession(request.EventSessionId, cancellationToken);
        return mapper.Map<List<EventSessionAgendaItemListDto>>(items);
    }
}
