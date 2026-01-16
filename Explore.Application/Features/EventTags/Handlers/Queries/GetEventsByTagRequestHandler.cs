using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventTags.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventTags.Handlers.Queries
{
    public class GetEventsByTagRequestHandler : IRequestHandler<GetEventsByTagRequest, List<EventListDto>>
    {
        private readonly IEventTagsRepository _eventTagsRepository;
        private readonly IMapper _mapper;

        public GetEventsByTagRequestHandler(IEventTagsRepository eventTagsRepository, IMapper mapper)
        {
            _eventTagsRepository = eventTagsRepository;
            _mapper = mapper;
        }

        public async Task<List<EventListDto>> Handle(GetEventsByTagRequest request, CancellationToken cancellationToken)
        {
            var events = await _eventTagsRepository.GetEventsByTag(request.TagId);
            return _mapper.Map<List<EventListDto>>(events);
        }
    }
}
