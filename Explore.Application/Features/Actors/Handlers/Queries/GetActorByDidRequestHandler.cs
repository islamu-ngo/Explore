using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorByDidRequestHandler : IRequestHandler<GetActorByDidRequest, ActorDto>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public GetActorByDidRequestHandler(IActorRepository actorRepository, IMapper mapper)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
    }

    public async Task<ActorDto> Handle(GetActorByDidRequest request, CancellationToken cancellationToken)
    {
        var actor = await _actorRepository.GetActorByDid(request.Did);
        return _mapper.Map<ActorDto>(actor);
    }
}
