// ABOUTME: Maps exact organizer-facing program-section reads from unrestricted tenant-safe repositories.
// ABOUTME: Detail reads verify parent-event ownership before returning physical location fields.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Features.EventSessionGroups.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionGroups.Handlers.Queries;

public sealed class GetManagedEventSessionGroupsByEventRequestHandler(
    IEventSessionGroupRepository repository,
    IMapper mapper)
    : IRequestHandler<GetManagedEventSessionGroupsByEventRequest, List<EventSessionGroupListDto>>
{
    public async Task<List<EventSessionGroupListDto>> Handle(
        GetManagedEventSessionGroupsByEventRequest request,
        CancellationToken cancellationToken)
    {
        var groups = await repository.GetActiveByEventAsync(request.EventId, cancellationToken);
        return mapper.Map<List<EventSessionGroupListDto>>(groups);
    }
}

public sealed class GetManagedEventSessionGroupDetailRequestHandler(
    IEventSessionGroupRepository repository,
    IMapper mapper)
    : IRequestHandler<GetManagedEventSessionGroupDetailRequest, EventSessionGroupDto?>
{
    public async Task<EventSessionGroupDto?> Handle(
        GetManagedEventSessionGroupDetailRequest request,
        CancellationToken cancellationToken)
    {
        var group = await repository.GetWithDetailsAsync(request.Id, cancellationToken);
        return group?.EventId == request.EventId
            ? mapper.Map<EventSessionGroupDto>(group)
            : null;
    }
}
