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
    public class GetLanguagesBySessionRequestHandler : IRequestHandler<GetLanguagesBySessionRequest, List<EventSessionLanguageListDto>>
    {
        private readonly IEventSessionLanguageRepository _sessionLanguageRepository;
        private readonly IMapper _mapper;

        public GetLanguagesBySessionRequestHandler(
            IEventSessionLanguageRepository sessionLanguageRepository,
            IMapper mapper)
        {
            _sessionLanguageRepository = sessionLanguageRepository;
            _mapper = mapper;
        }

        public async Task<List<EventSessionLanguageListDto>> Handle(GetLanguagesBySessionRequest request, CancellationToken cancellationToken)
        {
            var sessionLanguages = await _sessionLanguageRepository.GetBySession(request.EventSessionId);
            return _mapper.Map<List<EventSessionLanguageListDto>>(sessionLanguages);
        }
    }
}
