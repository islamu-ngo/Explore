using MediatR;
using Explore.Application.DTOs.Actor;

namespace Explore.Application.Features.Actors.Requests.Queries;

public class GetActorListRequest : IRequest<List<ActorListDto>>
{
}
