using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries;

public class GetRegistrationsByUserRequestHandler : IRequestHandler<GetRegistrationsByUserRequest, List<EventRegistrationListDto>>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;

    public GetRegistrationsByUserRequestHandler(IEventRegistrationRepository eventRegistrationRepository, IMapper mapper)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
    }

    public async Task<List<EventRegistrationListDto>> Handle(GetRegistrationsByUserRequest request, CancellationToken cancellationToken)
    {
        var registrations = await _eventRegistrationRepository.GetRegistrationsByUser(request.UserId);
        return _mapper.Map<List<EventRegistrationListDto>>(registrations);
    }
}
