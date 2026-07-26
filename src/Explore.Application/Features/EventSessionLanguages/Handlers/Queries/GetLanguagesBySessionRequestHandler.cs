// ABOUTME: Query handler returning all languages spoken in a specific event session.
// ABOUTME: Used for session language indicator display.
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
    private readonly IEventSessionRepository _eventSessionRepository;
    private readonly IMapper _mapper;

    public GetLanguagesBySessionRequestHandler(
        IEventSessionLanguageRepository repository,
        IEventSessionRepository eventSessionRepository,
        IMapper mapper)
    {
        _repository = repository;
        _eventSessionRepository = eventSessionRepository;
        _mapper = mapper;
    }

    public async Task<List<EventSessionLanguageListDto>> Handle(GetLanguagesBySessionRequest request, CancellationToken cancellationToken)
    {
        var eventSessionLanguages = await _repository.GetBySession(request.EventSessionId, cancellationToken);
        var eventSession = await _eventSessionRepository.GetById(request.EventSessionId);
        var dtos = _mapper.Map<List<EventSessionLanguageListDto>>(eventSessionLanguages);
        foreach (var dto in dtos)
        {
            dto.EventId = eventSession?.EventId ?? Guid.Empty;
            dto.TenantId = eventSession?.TenantId ?? Guid.Empty;
        }

        return dtos;
    }
}
