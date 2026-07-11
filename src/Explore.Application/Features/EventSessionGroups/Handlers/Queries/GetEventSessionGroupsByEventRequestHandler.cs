// ABOUTME: Handler for listing program sections/tracks/devrooms belonging to an event.
// ABOUTME: Delegates tenant-safe entity reads to the repository and maps in Application.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Features.EventSessionGroups.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Queries;

public class GetEventSessionGroupsByEventRequestHandler : IRequestHandler<GetEventSessionGroupsByEventRequest, List<EventSessionGroupListDto>>
{
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IMapper _mapper;

    public GetEventSessionGroupsByEventRequestHandler(
        IEventSessionGroupRepository eventSessionGroupRepository,
        IMapper mapper)
    {
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _mapper = mapper;
    }

    public async Task<List<EventSessionGroupListDto>> Handle(GetEventSessionGroupsByEventRequest request, CancellationToken cancellationToken)
    {
        var groups = await _eventSessionGroupRepository.GetByEventAsync(request.EventId, cancellationToken);
        return _mapper.Map<List<EventSessionGroupListDto>>(groups);
    }
}
