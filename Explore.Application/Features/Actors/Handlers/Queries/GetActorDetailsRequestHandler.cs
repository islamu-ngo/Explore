using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorDetailsRequestHandler : IRequestHandler<GetActorDetailsRequest, ActorDto>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public GetActorDetailsRequestHandler(IActorRepository actorRepository, IMapper mapper)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
    }

    public async Task<ActorDto> Handle(GetActorDetailsRequest request, CancellationToken cancellationToken)
    {
        var actor = await _actorRepository.GetActorWithDetails(request.Id);
        return _mapper.Map<ActorDto>(actor);
    }
}
