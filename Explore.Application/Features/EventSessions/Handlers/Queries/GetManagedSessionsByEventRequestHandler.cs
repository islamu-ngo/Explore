// ABOUTME: Handles organizer-facing event session list reads without applying public visibility filters.
// ABOUTME: Keeps entity retrieval in repositories and DTO mapping in the Application handler.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries;

public class GetManagedSessionsByEventRequestHandler : IRequestHandler<GetManagedSessionsByEventRequest, List<EventSessionListDto>>
{
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IMapper _mapper;

    public GetManagedSessionsByEventRequestHandler(
        IEventSessionRepository eventSessionRepository,
        IMapper mapper)
    {
        _eventSessionRepository = eventSessionRepository;
        _mapper = mapper;
    }

    public async Task<List<EventSessionListDto>> Handle(
        GetManagedSessionsByEventRequest request,
        CancellationToken cancellationToken)
    {
        var eventSessions = await _eventSessionRepository.GetSessionsByEvent(request.EventId);
        return _mapper.Map<List<EventSessionListDto>>(eventSessions);
    }
}
