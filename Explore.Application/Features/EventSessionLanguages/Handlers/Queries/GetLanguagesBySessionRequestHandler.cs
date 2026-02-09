using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Queries;

public class GetLanguagesBySessionRequestHandler : IRequestHandler<GetLanguagesBySessionRequest, List<EventSessionLanguageListDto>>
{
    private readonly IEventSessionLanguageRepository _repository;
    private readonly IMapper _mapper;

    public GetLanguagesBySessionRequestHandler(IEventSessionLanguageRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<EventSessionLanguageListDto>> Handle(GetLanguagesBySessionRequest request, CancellationToken cancellationToken)
    {
        var eventSessionLanguages = await _repository.GetBySession(request.EventSessionId);
        return _mapper.Map<List<EventSessionLanguageListDto>>(eventSessionLanguages);
    }
}
