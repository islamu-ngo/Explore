// ABOUTME: Handler for publicly listing program sections/tracks/devrooms belonging to an event.
// ABOUTME: Maps published groups while redacting exact physical location and room fields.

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
        var groups = await _eventSessionGroupRepository.GetPublicByEventAsync(request.EventId, cancellationToken);
        var dtos = _mapper.Map<List<EventSessionGroupListDto>>(groups);

        foreach (var dto in dtos)
        {
            dto.LocationId = null;
            dto.LocationName = null;
            dto.RoomId = null;
            dto.RoomName = null;
        }

        return dtos;
    }
}
