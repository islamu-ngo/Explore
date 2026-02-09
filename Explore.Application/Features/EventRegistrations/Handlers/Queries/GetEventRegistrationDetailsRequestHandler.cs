using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries;

public class GetEventRegistrationDetailsRequestHandler : IRequestHandler<GetEventRegistrationDetailsRequest, EventRegistrationDto>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;

    public GetEventRegistrationDetailsRequestHandler(IEventRegistrationRepository eventRegistrationRepository, IMapper mapper)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _mapper = mapper;
    }

    public async Task<EventRegistrationDto> Handle(GetEventRegistrationDetailsRequest request, CancellationToken cancellationToken)
    {
        var eventRegistration = await _eventRegistrationRepository.GetById(request.Id);
        return _mapper.Map<EventRegistrationDto>(eventRegistration);
    }
}
