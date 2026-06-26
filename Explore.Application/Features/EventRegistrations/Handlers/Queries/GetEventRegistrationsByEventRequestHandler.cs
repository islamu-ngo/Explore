// ABOUTME: Query handler returning paginated event registrations for one event.
// ABOUTME: Keeps event-scoped registration mapping in Application instead of Persistence or MCP.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries;

public class GetEventRegistrationsByEventRequestHandler
    : IRequestHandler<GetEventRegistrationsByEventRequest, PaginatedResult<EventRegistrationListDto>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;

    public GetEventRegistrationsByEventRequestHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IMapper mapper)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<EventRegistrationListDto>> Handle(
        GetEventRegistrationsByEventRequest request,
        CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = PaginatedResult<EventRegistrationListDto>.NormalizeParameters(
            request.PageNumber,
            request.PageSize);
        var (eventRegistrations, totalCount) =
            await _eventRegistrationRepository.GetRegistrationsByEventWithDetailsPaged(
                request.EventId,
                pageNumber,
                pageSize,
                cancellationToken);
        var dtos = _mapper.Map<List<EventRegistrationListDto>>(eventRegistrations);

        return PaginatedResult<EventRegistrationListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }
}
