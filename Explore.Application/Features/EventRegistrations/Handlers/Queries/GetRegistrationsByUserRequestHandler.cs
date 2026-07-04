// ABOUTME: Query handler returning all event registrations for a specific user.
// ABOUTME: Used for My Registrations user profile view.
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

public class GetRegistrationsByUserRequestHandler : IRequestHandler<GetRegistrationsByUserRequest, List<EventRegistrationListDto>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetRegistrationsByUserRequestHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<List<EventRegistrationListDto>> Handle(GetRegistrationsByUserRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException(ResourceKinds.EventRegistration, AuthorizationActions.View);
        if (request.UserId != currentUserId)
        {
            throw new AuthorizationException(ResourceKinds.EventRegistration, AuthorizationActions.View);
        }

        var registrations = await _eventRegistrationRepository.GetRegistrationsByUser(request.UserId, cancellationToken);
        return _mapper.Map<List<EventRegistrationListDto>>(registrations);
    }
}
