using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries
{
    public class GetEventRegistrationListRequestHandler : IRequestHandler<GetEventRegistrationListRequest, List<EventRegistrationListDto>>
    {
        private readonly IEventRegistrationRepository _eventRegistrationRepository;
        private readonly IMapper _mapper;

        public GetEventRegistrationListRequestHandler(IEventRegistrationRepository eventRegistrationRepository, IMapper mapper)
        {
            _eventRegistrationRepository = eventRegistrationRepository;
            _mapper = mapper;
        }

        public async Task<List<EventRegistrationListDto>> Handle(GetEventRegistrationListRequest request, CancellationToken cancellationToken)
        {
            var eventRegistrations = await _eventRegistrationRepository.GetAll();
            return _mapper.Map<List<EventRegistrationListDto>>(eventRegistrations);
        }
    }
}
