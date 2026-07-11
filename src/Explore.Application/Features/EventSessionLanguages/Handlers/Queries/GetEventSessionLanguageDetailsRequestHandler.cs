// ABOUTME: Query handler returning a single session-language link by ID.
// ABOUTME: Maps junction entity to EventSessionLanguageDto.
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionLanguages.Handlers.Queries;

public class GetEventSessionLanguageDetailsRequestHandler : IRequestHandler<GetEventSessionLanguageDetailsRequest, EventSessionLanguageDto>
{
    private readonly IEventSessionLanguageRepository _repository;
    private readonly IMapper _mapper;

    public GetEventSessionLanguageDetailsRequestHandler(IEventSessionLanguageRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<EventSessionLanguageDto> Handle(GetEventSessionLanguageDetailsRequest request, CancellationToken cancellationToken)
    {
        var eventSessionLanguage = await _repository.GetById(request.Id);
        return _mapper.Map<EventSessionLanguageDto>(eventSessionLanguage);
    }
}
