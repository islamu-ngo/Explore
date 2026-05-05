// ABOUTME: Handler for event session group detail retrieval.
// ABOUTME: Returns null for missing or tenant-filtered groups and maps entity data in Application.

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
        var group = await _eventSessionGroupRepository.GetWithDetailsAsync(request.Id, cancellationToken);
        return group is null ? null : _mapper.Map<EventSessionGroupDto>(group);
    }
}
