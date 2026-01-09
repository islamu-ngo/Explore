using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Queries
{
    public class GetEventSessionLanguageListRequestHandler : IRequestHandler<GetEventSessionLanguageListRequest, List<EventSessionLanguageListDto>>
    {
        private readonly IEventSessionLanguageRepository _repository;
        private readonly IMapper _mapper;

        public GetEventSessionLanguageListRequestHandler(IEventSessionLanguageRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<EventSessionLanguageListDto>> Handle(GetEventSessionLanguageListRequest request, CancellationToken cancellationToken)
        {
            var eventSessionLanguages = await _repository.GetAllAsync();
            return _mapper.Map<List<EventSessionLanguageListDto>>(eventSessionLanguages);
        }
    }
}
