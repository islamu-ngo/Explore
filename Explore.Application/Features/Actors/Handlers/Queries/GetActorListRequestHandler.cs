using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorListRequestHandler : IRequestHandler<GetActorListRequest, List<ActorListDto>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public GetActorListRequestHandler(IActorRepository actorRepository, IMapper mapper)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
    }

    public async Task<List<ActorListDto>> Handle(GetActorListRequest request, CancellationToken cancellationToken)
    {
        var actors = await _actorRepository.GetAll();
        return _mapper.Map<List<ActorListDto>>(actors);
    }
}
