// ABOUTME: Query handler returning a paginated list of event registrations.
// ABOUTME: Maps entities to EventRegistrationListDto.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries;

public class GetEventRegistrationListRequestHandler : IRequestHandler<GetEventRegistrationListRequest, PaginatedResult<EventRegistrationListDto>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetEventRegistrationListRequestHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResult<EventRegistrationListDto>> Handle(GetEventRegistrationListRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException("Event registration reads require an authenticated user.");
        var (pageNumber, pageSize) = PaginatedResult<EventRegistrationListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
        var (eventRegistrations, totalCount) = await _eventRegistrationRepository.GetRegistrationsByUserWithDetailsPaged(
            currentUserId,
            pageNumber,
            pageSize,
            cancellationToken);
        var dtos = _mapper.Map<List<EventRegistrationListDto>>(eventRegistrations);
        return PaginatedResult<EventRegistrationListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }
}
