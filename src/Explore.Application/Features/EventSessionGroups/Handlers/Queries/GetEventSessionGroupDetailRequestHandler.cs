// ABOUTME: Handler for public event session group detail retrieval.
// ABOUTME: Maps published data while redacting exact physical location and room fields.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Features.EventSessionGroups.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Queries;

public class GetEventSessionGroupDetailRequestHandler : IRequestHandler<GetEventSessionGroupDetailRequest, EventSessionGroupDto?>
{
    private readonly IEventSessionGroupRepository _eventSessionGroupRepository;
    private readonly IMapper _mapper;

    public GetEventSessionGroupDetailRequestHandler(
        IEventSessionGroupRepository eventSessionGroupRepository,
        IMapper mapper)
    {
        _eventSessionGroupRepository = eventSessionGroupRepository;
        _mapper = mapper;
    }

    public async Task<EventSessionGroupDto?> Handle(GetEventSessionGroupDetailRequest request, CancellationToken cancellationToken)
    {
        var group = await _eventSessionGroupRepository.GetPublicWithDetailsAsync(request.Id, cancellationToken);
        if (group is null)
            return null;

        var dto = _mapper.Map<EventSessionGroupDto>(group);
        dto.LocationId = null;
        dto.LocationName = null;
        dto.RoomId = null;
        dto.RoomName = null;
        return dto;
    }
}
