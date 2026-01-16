using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Actor;
using Explore.Application.Features.Actors.Requests.Queries;
using Explore.Application.Responses;

namespace Explore.Application.Features.Actors.Handlers.Queries;

public class GetActorListRequestHandler : IRequestHandler<GetActorListRequest, PaginatedResult<ActorListDto>>
{
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;

    public GetActorListRequestHandler(IActorRepository actorRepository, IMapper mapper)
    {
        _actorRepository = actorRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ActorListDto>> Handle(GetActorListRequest request, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = PaginatedResult<ActorListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
        var (actors, totalCount) = await _actorRepository.GetActorsWithDetailsPaged(pageNumber, pageSize);
        var dtos = _mapper.Map<List<ActorListDto>>(actors);
        return PaginatedResult<ActorListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }
}
