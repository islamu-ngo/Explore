using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventCategories.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventCategories.Handlers.Queries
{
    public class GetEventsByCategoryRequestHandler : IRequestHandler<GetEventsByCategoryRequest, List<EventListDto>>
    {
        private readonly IEventCategoriesRepository _eventCategoriesRepository;
        private readonly IMapper _mapper;

        public GetEventsByCategoryRequestHandler(IEventCategoriesRepository eventCategoriesRepository, IMapper mapper)
        {
            _eventCategoriesRepository = eventCategoriesRepository;
            _mapper = mapper;
        }

        public async Task<List<EventListDto>> Handle(GetEventsByCategoryRequest request, CancellationToken cancellationToken)
        {
            var events = await _eventCategoriesRepository.GetEventsByCategory(request.CategoryId);
            return _mapper.Map<List<EventListDto>>(events);
        }
    }
}
