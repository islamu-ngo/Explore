using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Queries
{
    public class GetEventSessionLanguageDetailsRequestHandler : IRequestHandler<GetEventSessionLanguageDetailsRequest, EventSessionLanguageDto>
    {
        private readonly IEventSessionLanguageRepository _sessionLanguageRepository;
        private readonly IMapper _mapper;

        public GetEventSessionLanguageDetailsRequestHandler(
            IEventSessionLanguageRepository sessionLanguageRepository,
            IMapper mapper)
        {
            _sessionLanguageRepository = sessionLanguageRepository;
            _mapper = mapper;
        }

        public async Task<EventSessionLanguageDto> Handle(GetEventSessionLanguageDetailsRequest request, CancellationToken cancellationToken)
        {
            var sessionLanguage = await _sessionLanguageRepository.GetById(request.Id);
            return _mapper.Map<EventSessionLanguageDto>(sessionLanguage);
        }
    }
}
