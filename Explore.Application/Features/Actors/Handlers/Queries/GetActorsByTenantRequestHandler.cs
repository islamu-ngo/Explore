using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorsByTenantRequestHandler : IRequestHandler<GetActorsByTenantRequest, List<ActorListDto>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public GetActorsByTenantRequestHandler(IActorRepository actorRepository, IMapper mapper)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
    }

    public async Task<List<ActorListDto>> Handle(GetActorsByTenantRequest request, CancellationToken cancellationToken)
    {
        var actors = await _actorRepository.GetActorsByTenant(request.TenantId);
        return _mapper.Map<List<ActorListDto>>(actors);
    }
}
