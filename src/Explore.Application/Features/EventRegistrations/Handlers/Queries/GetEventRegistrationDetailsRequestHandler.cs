// ABOUTME: Query handler returning the current user's event registration by ID.
// ABOUTME: Enforces owner scoping before mapping EventRegistration to EventRegistrationDto.
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

public class GetEventRegistrationDetailsRequestHandler : IRequestHandler<GetEventRegistrationDetailsRequest, EventRegistrationDto?>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetEventRegistrationDetailsRequestHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<EventRegistrationDto?> Handle(GetEventRegistrationDetailsRequest request, CancellationToken cancellationToken)
    {
        var eventRegistration = await _eventRegistrationRepository.GetByIdWithDetails(request.Id, cancellationToken);
        if (eventRegistration is null)
        {
            return null;
        }

        var currentUserId = _currentUserService.UserId
            ?? throw new AuthorizationException(ResourceKinds.EventRegistration, AuthorizationActions.View);
        if (eventRegistration.UserId != currentUserId)
        {
            throw new AuthorizationException(ResourceKinds.EventRegistration, AuthorizationActions.View);
        }

        return _mapper.Map<EventRegistrationDto>(eventRegistration);
    }
}
