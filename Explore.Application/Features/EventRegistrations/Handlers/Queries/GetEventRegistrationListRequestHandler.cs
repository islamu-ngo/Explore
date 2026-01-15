using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Queries
{
    public class GetEventRegistrationListRequestHandler : IRequestHandler<GetEventRegistrationListRequest, PaginatedResult<EventRegistrationListDto>>
    {
        private readonly IEventRegistrationRepository _eventRegistrationRepository;
        private readonly IMapper _mapper;

        public GetEventRegistrationListRequestHandler(IEventRegistrationRepository eventRegistrationRepository, IMapper mapper)
        {
            _eventRegistrationRepository = eventRegistrationRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<EventRegistrationListDto>> Handle(GetEventRegistrationListRequest request, CancellationToken cancellationToken)
        {
            var (pageNumber, pageSize) = PaginatedResult<EventRegistrationListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
            var (eventRegistrations, totalCount) = await _eventRegistrationRepository.GetRegistrationsWithDetailsPaged(pageNumber, pageSize);
            var dtos = _mapper.Map<List<EventRegistrationListDto>>(eventRegistrations);
            return PaginatedResult<EventRegistrationListDto>.Create(dtos, totalCount, pageNumber, pageSize);
        }
    }
}
