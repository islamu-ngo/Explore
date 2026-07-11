// ABOUTME: Query handler returning the current user's registration for a specific event session.
// ABOUTME: Prevents broad attendee enumeration through the generic session registration route.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries;

public class GetRegistrationsBySessionRequestHandler : IRequestHandler<GetRegistrationsBySessionRequest, List<EventRegistrationListDto>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetRegistrationsBySessionRequestHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<List<EventRegistrationListDto>> Handle(GetRegistrationsBySessionRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException(ResourceKinds.EventRegistration, AuthorizationActions.View);
        var registration = await _eventRegistrationRepository.GetRegistrationByUserAndSession(
            currentUserId,
            request.EventSessionId,
            cancellationToken);
        return registration is null
            ? new List<EventRegistrationListDto>()
            : new List<EventRegistrationListDto> { _mapper.Map<EventRegistrationListDto>(registration) };
    }
}
